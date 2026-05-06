using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Application.Models;
using CoreModels = Luno.MissionControl.Core.Models;
using Luno.SDK;
using Luno.SDK.Application.Account;
using Luno.SDK.Application.Market;
using Luno.SDK.Application.Trading;
using Luno.SDK.Trading;

namespace Luno.MissionControl.Infrastructure.Adapters;

/// <summary>
/// Infrastructure adapter that bridges the Luno SDK with the domain's Application Ports.
/// Provides a decoupled implementation of market data and trading operations.
/// </summary>
public sealed class LunoSdkBridge(ILunoClient lunoClient) : ILunoTrader, ILunoMarketData
{
    public async Task<IReadOnlyList<CoreModels.AccountBalance>> GetBalancesAsync(CancellationToken ct = default)
    {
        var balances = await lunoClient.Accounts.GetBalancesAsync(new GetBalancesQuery(), ct);
        return balances?.Select(b => new CoreModels.AccountBalance(b.Asset, b.Available, b.AccountId)).ToList() ?? [];
    }

    public async Task<IReadOnlyList<CoreModels.MarketMetadata>> GetMarketsAsync(IEnumerable<string> pairs, CancellationToken ct = default)
    {
        var markets = await lunoClient.Market.GetMarketsAsync(new GetMarketsQuery { Pairs = pairs.ToArray() }, ct);
        return markets?.Select(m => new CoreModels.MarketMetadata(
            m.Pair, 
            m.BaseCurrency, 
            m.CounterCurrency, 
            m.PriceScale, 
            m.VolumeScale, 
            m.MinVolume, 
            m.MinPrice)).ToList() ?? [];
    }

    public async Task<CoreModels.OrderEstimation> EstimateOrderAsync(string pair, decimal spend, CancellationToken ct = default)
    {
        var quote = await lunoClient.Trading.CalculateOrderSizeAsync(
            new CalculateOrderSizeQuery(pair, OrderSide.Buy, TradingAmount.InQuote(spend)), ct);
        
        return new CoreModels.OrderEstimation(pair, quote.Volume, quote.Price, spend);
    }

    public async Task<string> PostOrderAsync(CoreModels.OrderEstimation estimation, long baseAccountId, long counterAccountId, CancellationToken ct = default)
    {

        var sdkQuote = await lunoClient.Trading.CalculateOrderSizeAsync(
            new CalculateOrderSizeQuery(estimation.Pair, OrderSide.Buy, TradingAmount.InQuote(estimation.TotalSpend)), ct);

        var command = sdkQuote.ToCommand(baseAccountId, counterAccountId, System.Guid.NewGuid().ToString()) with
        {
            Options = new LunoRequestOptions { AuthorizeWriteOperation = true }
        };

        var response = await lunoClient.Trading.PostLimitOrderAsync(command, ct);
        return response.OrderId;
    }
}
