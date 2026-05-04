using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
public sealed class BasketOrchestrator : IBasketService, ICommandHandler<ExecuteAllocationCommand, BasketExecutionResult>
{
    private readonly ILunoClient _lunoClient;
    private readonly ILogger<BasketOrchestrator> _logger;

    public BasketOrchestrator(ILunoClient lunoClient, ILogger<BasketOrchestrator> logger)
    {
        _lunoClient = lunoClient ?? throw new ArgumentNullException(nameof(lunoClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BasketExecutionResult> ExecuteAsync(BasketExecutionRequest request, CancellationToken ct = default)
    {
        var command = new ExecuteAllocationCommand(request.TotalSpend, request.Allocations);
        return await HandleAsync(command, ct);
    }

    public async Task<BasketExecutionResult> HandleAsync(ExecuteAllocationCommand command, CancellationToken ct = default)
    {
        using var activity = ForensicTracing.StartActivity("ExecuteBasket");
        activity?.SetTag("basket.total_spend", command.TotalSpend);
        activity?.SetTag("basket.asset_count", command.Allocations.Count);
        ValidateWeights(command.Allocations);

        var pairs = command.Allocations.Select(a => a.Pair).ToArray();
        var markets = await _lunoClient.Market.GetMarketsAsync(new GetMarketsQuery { Pairs = pairs }, ct)
            ?? throw new InvalidOperationException("SDK returned a null market response. Verify market pairs and API connectivity.");

        var marketMap = markets.ToDictionary(m => m.Pair);

        var balances = await _lunoClient.Accounts.GetBalancesAsync(new GetBalancesQuery(), ct)
            ?? throw new InvalidOperationException("SDK returned a null balance response. Verify account access and credentials.");

        var orderResponses = new List<OrderSummary>();

        foreach (var allocation in command.Allocations)
        {
            if (!marketMap.TryGetValue(allocation.Pair, out var market))
            {
                throw new InvalidOperationException($"Market metadata for {allocation.Pair} was not found.");
            }

            var baseAccounts = balances
                .Where(a => a.Asset == market.BaseCurrency)
                .OrderBy(a => a.Available)
                .ToList();

            var counterAccount = balances
                .OrderBy(a => a.Available)
                .FirstOrDefault(a => a.Asset == market.CounterCurrency)
                ?? throw new InvalidOperationException($"No {market.CounterCurrency} account found.");

            if (!baseAccounts.Any())
                throw new InvalidOperationException($"No {market.BaseCurrency} account found.");

            var counterAccountId = long.Parse(counterAccount.AccountId);
            bool orderPlaced = false;
            string lastError = string.Empty;

            foreach (var baseAcc in baseAccounts)
            {
                var baseAccountId = long.Parse(baseAcc.AccountId);
                
                decimal portionSpend = command.TotalSpend * allocation.Weight;

                var quote = await _lunoClient.Trading.CalculateOrderSizeAsync(
                    new CalculateOrderSizeQuery(
                        Pair: allocation.Pair,
                        Side: OrderSide.Buy,
                        Spend: TradingAmount.InQuote(portionSpend)
                    ), ct);

                var postCommand = quote.ToCommand(
                    baseAccountId: baseAccountId,
                    counterAccountId: counterAccountId,
                    clientOrderId: Guid.NewGuid().ToString()
                ) with
                {
                    Options = new LunoRequestOptions { AuthorizeWriteOperation = true }
                };

                try 
                {
                    _logger.LogInformation("Executing order to buy {Volume} {BaseAsset} for {Price} {CounterAsset} (Spend: {PortionSpend}, BaseAcc: {BaseAccountId}, CounterAcc: {CounterAccountId})", 
                        quote.Volume, market.BaseCurrency, quote.Price, market.CounterCurrency, portionSpend, baseAccountId, counterAccountId);

                    var response = await _lunoClient.Trading.PostLimitOrderAsync(postCommand, ct);
                    orderResponses.Add(new OrderSummary(response.OrderId, allocation.Pair));
                    orderPlaced = true;
                    break;
                }
                catch (Exception ex) when (ex.Message.Contains("400") || ex.Message.Contains("ErrInvalidAccount"))
                {
                    _logger.LogWarning("Account {BaseAccountId} rejected the order (400). Trying next available account for {BaseAsset}...", baseAccountId, market.BaseCurrency);
                    lastError = ex.Message;
                    continue; // Try the next account
                }
            }

            if (!orderPlaced)
            {
                throw new InvalidOperationException($"Failed to place order for {allocation.Pair} after trying {baseAccounts.Count} accounts. Last error: {lastError}");
            }

            // D: Add a "Polite Pacing" delay to avoid rate limits and allow balance synchronization
            // We use CancellationToken.None to ensure pacing completes even if the client disconnects.
            if (allocation != command.Allocations.Last())
            {
                _logger.LogDebug("Waiting 30 seconds before next allocation to ensure API stability...");
                await Task.Delay(TimeSpan.FromSeconds(30), CancellationToken.None);
            }
        }

        return new BasketExecutionResult(true, orderResponses);
    }

    private static void ValidateWeights(IReadOnlyList<Allocation> allocations)
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
