using Luno.MissionControl.Application.Models;
using Luno.MissionControl.Application;
using Luno.MissionControl.Application.Ports;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Luno.MissionControl.Web.Services;

/// <summary>
/// A Server-side implementation of the price state that consumes updates DIRECTLY 
/// from the internal IPriceBroadcaster, avoiding the SignalR loopback deadlock.
/// </summary>
public class ServerBasketState : IBasketState, IDisposable
{
    private readonly IPriceBroadcaster _broadcaster;
    private readonly MarketInventory _marketInventory;
    private readonly ConcurrentDictionary<string, TickerSnapshot> _prices = [];
    
    public event Action<TickerSnapshot>? OnPriceUpdate;
    public event Action<IReadOnlyList<MarketMetadata>>? OnMarketsUpdate;

    public IReadOnlyDictionary<string, TickerSnapshot> Prices => _prices;
    public IReadOnlyList<MarketMetadata> AvailableMarkets => _marketInventory.GetMarkets();

    public string SelectedCurrency { get; set; } = "MYR";
    public decimal TargetSpend { get; set; } = 1000m;
    public long BaseAccountId { get; set; }
    public long CounterAccountId { get; set; }

    public ServerBasketState(IPriceBroadcaster broadcaster, MarketInventory marketInventory)
    {
        _broadcaster = broadcaster ?? throw new ArgumentNullException(nameof(broadcaster));
        _marketInventory = marketInventory ?? throw new ArgumentNullException(nameof(marketInventory));
        _broadcaster.OnPriceUpdate += HandleBroadcasterUpdate;
    }

    private void HandleBroadcasterUpdate(TickerSnapshot snapshot)
    {
        _prices[snapshot.Pair] = snapshot;
        OnPriceUpdate?.Invoke(snapshot);
    }

    public Task ReceivePriceUpdate(TickerSnapshot snapshot)
    {
        // Compatibility implementation for the IPriceClient interface
        HandleBroadcasterUpdate(snapshot);
        return Task.CompletedTask;
    }

    public Task ReceiveMarketMetadata(IReadOnlyList<MarketMetadata> markets)
    {
        OnMarketsUpdate?.Invoke(markets);
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        // No-op: The server-side state is always "connected" to the internal broadcaster
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _broadcaster.OnPriceUpdate -= HandleBroadcasterUpdate;
    }
}
