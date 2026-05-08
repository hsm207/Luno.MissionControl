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
    ILunoAccountAdapter accountAdapter,
    IWalletRepository walletRepository,
    Luno.MissionControl.Core.Services.WalletResolver resolver,
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

            // 3. Fetch Live Accounts (Bridged via Adapter - Grouped by Asset)
            var groupedAccounts = await accountAdapter.GetAccountsAsync(ct);

            // 4. Sequential Execution
            foreach (var allocation in basket.Allocations)
            {
                if (!marketMap.TryGetValue(allocation.Pair, out var market))
                {
                    throw new InvalidOperationException($"Market metadata for {allocation.Pair} was not found.");
                }

                // 5. Deterministic Wallet Resolution (Zero-Ambiguity Mandate)
                var basePreference = await walletRepository.GetPreferenceAsync(market.BaseCurrency, ct);
                var counterPreference = await walletRepository.GetPreferenceAsync(market.CounterCurrency, ct);

                groupedAccounts.TryGetValue(market.BaseCurrency, out var baseCandidates);
                groupedAccounts.TryGetValue(market.CounterCurrency, out var counterCandidates);

                var baseAccount = resolver.Resolve(baseCandidates ?? [], market.BaseCurrency, basePreference, isBase: true);
                var counterAccount = resolver.Resolve(counterCandidates ?? [], market.CounterCurrency, counterPreference, isBase: false);

                // 6. Obtain a domain-aligned estimation
                var estimation = await trader.EstimateOrderAsync(allocation.Pair, allocation.TargetSpend, ct);

                logger.LogInformation("Executing order to buy {Volume} {BaseAsset} for {Price} {CounterAsset} (Spend: {PortionSpend}, BaseAcc: {BaseAccountId}, CounterAcc: {CounterAccountId})",
                    estimation.Volume, market.BaseCurrency, estimation.Price, market.CounterCurrency, allocation.TargetSpend, baseAccount.Id, counterAccount.Id);

                // 7. Execute the order via the trader abstraction
                var orderId = await trader.PostOrderAsync(estimation, baseAccount.Id, counterAccount.Id, ct);

                orderSummaries.Add(new OrderSummary(orderId, allocation.Pair));

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
            logger.LogError(ex, "Basket execution failed for {TotalSpend}. Reason: {Message}", command.TotalSpend, ex.Message);
            return new BasketExecutionResponse(false, orderSummaries, ex.Message);
        }
    }
}

