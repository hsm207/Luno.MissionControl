using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Application.Commands;
using Luno.MissionControl.Application.Models;
using Luno.MissionControl.Application.Diagnostics;
using Luno.MissionControl.Core;
using Luno.MissionControl.Core.Models;

namespace Luno.MissionControl.Application.UseCases;

/// <summary>
/// Orchestrates the execution of a multi-asset smart basket.
/// Handles validation and sequential order placement via decoupled domain abstractions.
/// </summary>
public sealed class BasketOrchestrator(
    ILunoTrader trader,
    ILunoMarketData marketData,
    ILogger<BasketOrchestrator> logger)
    : IBasketService
{
    public async Task<BasketExecutionResponse> ExecuteAsync(ExecuteAllocationCommand command, CancellationToken ct = default)
    {
        using var activity = ForensicTracing.StartActivity("BasketExecution");
        activity?.SetTag("spend.total", command.TotalSpend);

        logger.LogInformation("Order request received for {Total} counter currency across {Count} pairs.", command.TotalSpend, command.Allocations.Count);

        // 1. Domain Validation (Valid-by-Construction)
        var domainAllocations = command.Allocations
            .Select(a => new Allocation(a.Pair, new AllocationWeight(a.Weight * 100.0m)))
            .ToList();

        var basket = new OrderBasket(command.TotalSpend, domainAllocations);
        List<OrderSummary> orderSummaries = [];

        try
        {
            // 2. Resolve Market Metadata
            var pairs = basket.Allocations.Select(a => a.Pair).ToList();
            var markets = await marketData.GetMarketsAsync(pairs, ct);
            var marketMap = markets.ToDictionary(m => m.MarketId);

            // 3. Resolve Account Balances
            var balances = await marketData.GetBalancesAsync(ct);

            // 4. Sequential Execution
            foreach (var allocation in basket.Allocations)
            {
                if (!marketMap.TryGetValue(allocation.Pair, out var market))
                {
                    throw new InvalidOperationException($"Market metadata for {allocation.Pair} was not found.");
                }

                // Identify candidate accounts for base and counter assets
                var baseAccounts = balances
                    .Where(a => a.Asset == market.BaseCurrency)
                    .OrderBy(a => a.Available)
                    .ToList();

                var counterAccount = balances
                    .OrderBy(a => a.Available)
                    .FirstOrDefault(a => a.Asset == market.CounterCurrency)
                    ?? throw new InvalidOperationException($"No {market.CounterCurrency} account found for spend.");

                if (!baseAccounts.Any())
                    throw new InvalidOperationException($"No {market.BaseCurrency} account found for allocation.");

                var counterAccountId = long.Parse(counterAccount.AccountId);
                bool orderPlaced = false;
                string lastError = string.Empty;

                foreach (var baseAcc in baseAccounts)
                {
                    var baseAccountId = long.Parse(baseAcc.AccountId);

                    // 5. Obtain a domain-aligned estimation
                    var estimation = await trader.EstimateOrderAsync(allocation.Pair, allocation.TargetSpend, ct);

                    try
                    {
                        logger.LogInformation("Executing order to buy {Volume} {BaseAsset} for {Price} {CounterAsset} (Spend: {PortionSpend}, BaseAcc: {BaseAccountId}, CounterAcc: {CounterAccountId})",
                            estimation.Volume, market.BaseCurrency, estimation.Price, market.CounterCurrency, allocation.TargetSpend, baseAccountId, counterAccountId);

                        // 6. Execute the order via the trader abstraction
                        var orderId = await trader.PostOrderAsync(estimation, baseAccountId, counterAccountId, ct);

                        orderSummaries.Add(new OrderSummary(orderId, allocation.Pair));
                        orderPlaced = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Account {BaseAccountId} rejected the order. Trying next available account...", baseAccountId);
                        lastError = ex.Message;
                        continue;
                    }
                }

                if (!orderPlaced)
                {
                    throw new InvalidOperationException($"Failed to place order for {allocation.Pair} after trying {baseAccounts.Count} accounts. Last error: {lastError}");
                }

                // Polite Pacing
                if (allocation != basket.Allocations.Last())
                {
                    logger.LogDebug("Waiting 500ms before next allocation to ensure API stability...");
                    await Task.Delay(500, CancellationToken.None);
                }
            }

            return new BasketExecutionResponse(true, orderSummaries);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Basket execution failed for {TotalSpend}", command.TotalSpend);
            return new BasketExecutionResponse(false, orderSummaries, ex.Message);
        }
    }
}
