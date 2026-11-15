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
    public async Task<BasketExecutionResponseDto> ExecuteAsync(ExecuteAllocationCommand command, CancellationToken ct = default)
    {
        using var activity = ForensicTracing.StartActivity("BasketExecution");
        activity?.SetTag("spend.total", command.TotalSpend);

        logger.LogInformation("Order request received for {Total} counter currency across {Count} pairs.", command.TotalSpend, command.Allocations.Count);

        var domainAllocations = command.Allocations
            .Select(a => new Allocation(a.Pair, new AllocationWeight(a.Weight * 100.0m)))
            .ToList();

        var basket = new OrderBasket(command.TotalSpend, domainAllocations);
        List<OrderSummaryDto> orderSummaries = [];

        try
        {
            var resolvedAllocations = await ResolveAllocationsAsync(basket.Allocations, ct);
            await ExecuteResolvedAllocationsAsync(resolvedAllocations, orderSummaries, ct);

            return new BasketExecutionResponseDto(true, orderSummaries);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Basket execution failed for {TotalSpend}. Reason: {Message}", command.TotalSpend, ex.Message);
            return new BasketExecutionResponseDto(false, orderSummaries, ex.Message);
        }
    }

    private async Task<IReadOnlyList<ResolvedAllocation>> ResolveAllocationsAsync(
        IReadOnlyList<Allocation> allocations,
        CancellationToken ct)
    {
        var pairs = allocations.Select(a => a.Pair).ToList();
        var marketMap = (await marketData.GetMarketsAsync(pairs, ct)).ToDictionary(m => m.MarketId);
        var groupedAccounts = await accountAdapter.GetAccountsAsync(ct);
        var context = new SpotResolutionContext(groupedAccounts, [], ct);
        var resolved = new List<ResolvedAllocation>(allocations.Count);

        foreach (var allocation in allocations)
        {
            if (!marketMap.TryGetValue(allocation.Pair, out var market))
            {
                throw new InvalidOperationException($"Market metadata for {allocation.Pair} was not found.");
            }

            var baseAccountId = await ResolveCurrencySpotAccountAsync(market.BaseCurrency, context);
            var counterAccountId = await ResolveCurrencySpotAccountAsync(market.CounterCurrency, context);
            resolved.Add(new ResolvedAllocation(allocation, market, baseAccountId, counterAccountId));
        }

        return resolved;
    }

    private async Task<long> ResolveCurrencySpotAccountAsync(string currency, SpotResolutionContext context)
    {
        if (!context.Preferences.TryGetValue(currency, out var preference))
        {
            preference = await walletRepository.GetPreferenceAsync(currency, context.CancellationToken);
            context.Preferences[currency] = preference;
        }

        context.Accounts.TryGetValue(currency, out var candidates);
        return resolver.ResolveSpotAccountId(candidates ?? [], currency, preference);
    }

    private async Task ExecuteResolvedAllocationsAsync(
        IReadOnlyList<ResolvedAllocation> resolvedAllocations,
        List<OrderSummaryDto> orderSummaries,
        CancellationToken ct)
    {
        for (var i = 0; i < resolvedAllocations.Count; i++)
        {
            var (allocation, market, baseAccountId, counterAccountId) = resolvedAllocations[i];
            var estimation = await trader.EstimateOrderAsync(allocation.Pair, allocation.TargetSpend, ct);

            logger.LogInformation(
                "Executing order to buy {Volume} {BaseAsset} for {Price} {CounterAsset} " +
                "(Spend: {PortionSpend}, BaseAcc: {BaseAccountId}, CounterAcc: {CounterAccountId})",
                estimation.Volume,
                market.BaseCurrency,
                estimation.Price,
                market.CounterCurrency,
                allocation.TargetSpend,
                baseAccountId,
                counterAccountId);

            var orderId = await trader.PostOrderAsync(estimation, baseAccountId, counterAccountId, ct);
            orderSummaries.Add(new OrderSummaryDto(orderId, allocation.Pair));

            if (i < resolvedAllocations.Count - 1)
            {
                await Task.Delay(500, CancellationToken.None);
            }
        }
    }

    private sealed record SpotResolutionContext(
        IDictionary<string, List<LunoAccount>> Accounts,
        Dictionary<string, TradingAccountPreference?> Preferences,
        CancellationToken CancellationToken);

    private sealed record ResolvedAllocation(
        Allocation Allocation,
        MarketMetadata Market,
        long BaseAccountId,
        long CounterAccountId);
}
