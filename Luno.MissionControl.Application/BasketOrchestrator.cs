using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luno.MissionControl.Application.Commands;
using Luno.MissionControl.Application.Models;
using Luno.SDK;
using Luno.SDK.Application.Account;
using Luno.SDK.Application.Market;
using Luno.SDK.Application.Trading;
using Luno.SDK.Trading;

namespace Luno.MissionControl.Application;

/// <summary>
/// Orchestrates the execution of a multi-asset smart basket.
/// Handles validation, account resolution, and sequential order placement via the Luno SDK.
/// </summary>
public sealed class BasketOrchestrator : ICommandHandler<ExecuteAllocationCommand, BasketExecutionResult>
{
    private readonly ILunoClient _lunoClient;

    public BasketOrchestrator(ILunoClient lunoClient)
    {
        _lunoClient = lunoClient ?? throw new ArgumentNullException(nameof(lunoClient));
    }

    public async Task<BasketExecutionResult> HandleAsync(ExecuteAllocationCommand command, CancellationToken ct = default)
    {
        // 1. Validation: Weights must sum exactly to 1.00
        ValidateWeights(command.Allocations);

        // 2. Resolve Market Metadata
        var pairs = command.Allocations.Select(a => a.Pair).ToArray();
        var markets = await _lunoClient.Market.GetMarketsAsync(new GetMarketsQuery { Pairs = pairs }, ct)
            ?? throw new InvalidOperationException("SDK returned a null market response. Verify market pairs and API connectivity.");

        var marketMap = markets.ToDictionary(m => m.Pair);

        // 3. Resolve Account IDs using the Concept 6 pattern
        var balances = await _lunoClient.Accounts.GetBalancesAsync(new GetBalancesQuery(), ct)
            ?? throw new InvalidOperationException("SDK returned a null balance response. Verify account access and credentials.");

        var orderResponses = new List<OrderResponse>();

        // 4. Sequential Execution Phase
        foreach (var allocation in command.Allocations)
        {
            if (!marketMap.TryGetValue(allocation.Pair, out var market))
            {
                throw new InvalidOperationException($"Market metadata for {allocation.Pair} was not found.");
            }

            // Resolve Base and Counter account IDs manually to ensure descriptive errors
            var baseAccount = balances.FirstOrDefault(b => b.Asset == market.BaseCurrency)
                ?? throw new InvalidOperationException($"No {market.BaseCurrency} account found for {allocation.Pair} (Base).");
            
            var counterAccount = balances.FirstOrDefault(b => b.Asset == market.CounterCurrency)
                ?? throw new InvalidOperationException($"No {market.CounterCurrency} account found for {allocation.Pair} (Counter).");

            var baseAccountId = long.Parse(baseAccount.AccountId);
            var counterAccountId = long.Parse(counterAccount.AccountId);

            // Calculate the portion of total spend for this asset
            decimal portionSpend = command.TotalSpend * allocation.Weight;

            // A: Calculate optimal order size (Volume/Price)
            var quote = await _lunoClient.Trading.CalculateOrderSizeAsync(
                new CalculateOrderSizeQuery(
                    Pair: allocation.Pair,
                    Side: OrderSide.Buy,
                    Spend: TradingAmount.InQuote(portionSpend)
                ), ct);

            // B: Map to PostLimitOrderCommand using finalized quote
            var postCommand = quote.ToCommand(
                baseAccountId: baseAccountId,
                counterAccountId: counterAccountId,
                clientOrderId: Guid.NewGuid().ToString()
            ) with
            {
                Options = new LunoRequestOptions { AuthorizeWriteOperation = true }
            };

            // C: Place the order
            var response = await _lunoClient.Trading.PostLimitOrderAsync(postCommand, ct);
            orderResponses.Add(response);
        }

        return new BasketExecutionResult(true, orderResponses);
    }

    private static void ValidateWeights(IReadOnlyList<BasketAllocation> allocations)
    {
        if (allocations == null || allocations.Count == 0)
        {
            throw new ArgumentException("Basket must contain at least one allocation.", nameof(allocations));
        }

        var sum = allocations.Sum(a => a.Weight);
        if (sum != 1.00m)
        {
            throw new InvalidOperationException($"Invalid basket: Weights must sum exactly to 1.00 (Current sum: {sum}).");
        }
    }
}
