using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Application.Diagnostics;
using CoreModels = Luno.MissionControl.Core.Models;
using Luno.SDK;
using Luno.SDK.Application.Market;
using Luno.SDK.Application.Trading;
using Luno.SDK.Trading;
using Microsoft.Extensions.Logging;

namespace Luno.MissionControl.Infrastructure.Adapters;

/// <summary>
/// A unified infrastructure adapter that provides both market data and trading execution 
/// via the Luno SDK. Logical cohesion is maintained by grouping exchange-related operations.
/// </summary>
public sealed class LunoSdkExchangeAdapter(ILunoClient lunoClient, ILogger<LunoSdkExchangeAdapter> logger) 
    : ILunoMarketData, ILunoTrader
{
    public async Task<IReadOnlyList<CoreModels.MarketMetadata>> GetMarketsAsync(IEnumerable<string> pairs, CancellationToken ct = default)
    {
        using var activity = ForensicTracing.StartActivity("LunoSDK.GetMarkets");
        var pairList = pairs.ToArray();
        activity?.SetTag("luno.pairs", string.Join(",", pairList));

        try
        {
            var markets = await lunoClient.Market.GetMarketsAsync(new GetMarketsQuery { Pairs = pairList }, ct);
            return markets?.Select(m => new CoreModels.MarketMetadata(
                m.Pair,
                m.BaseCurrency,
                m.CounterCurrency,
                m.PriceScale,
                m.VolumeScale,
                m.MinVolume,
                m.MinPrice)).ToList() ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch market metadata from Luno SDK.");
            throw;
        }
    }

    public async Task<CoreModels.OrderEstimation> EstimateOrderAsync(string pair, decimal spend, CancellationToken ct = default)
    {
        using var activity = ForensicTracing.StartActivity("LunoSDK.EstimateOrder");
        activity?.SetTag("luno.pair", pair);
        activity?.SetTag("luno.spend", spend);

        try
        {
            var quote = await lunoClient.Trading.CalculateOrderSizeAsync(
                new CalculateOrderSizeQuery(pair, OrderSide.Buy, TradingAmount.InQuote(spend)), ct);

            return new CoreModels.OrderEstimation(pair, quote.Volume, quote.Price, spend);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to estimate order size for {Pair} from Luno SDK.", pair);
            throw;
        }
    }

    public async Task<string> PostOrderAsync(CoreModels.OrderEstimation estimation, long baseAccountId, long counterAccountId, CancellationToken ct = default)
    {
        using var activity = ForensicTracing.StartActivity("LunoSDK.PostOrder");
        activity?.SetTag("luno.pair", estimation.Pair);
        activity?.SetTag("luno.volume", estimation.Volume);

        try
        {
            // Re-calculate to ensure we have the latest SDK-specific command state
            var sdkQuote = await lunoClient.Trading.CalculateOrderSizeAsync(
                new CalculateOrderSizeQuery(estimation.Pair, OrderSide.Buy, TradingAmount.InQuote(estimation.TotalSpend)), ct);

            var command = sdkQuote.ToCommand(baseAccountId, counterAccountId, System.Guid.NewGuid().ToString()) with
            {
                Options = new LunoRequestOptions { AuthorizeWriteOperation = true }
            };

            var response = await lunoClient.Trading.PostLimitOrderAsync(command, ct);
            
            activity?.SetTag("luno.order_id", response.OrderId);
            ForensicMetrics.OrdersExecuted.Add(1, new KeyValuePair<string, object?>("pair", estimation.Pair));

            return response.OrderId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to post order for {Pair} to Luno SDK.", estimation.Pair);
            ForensicMetrics.ExecutionFailures.Add(1, new KeyValuePair<string, object?>("pair", estimation.Pair));
            throw;
        }
    }
}
