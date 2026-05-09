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

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerBasketState"/> class.
    /// Tethers the state directly to the internal price broadcaster.
    /// </summary>
    /// <param name="broadcaster">The internal price broadcasting engine.</param>
    /// <param name="marketInventory">The metadata inventory service.</param>
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

    /// <summary>
    /// Synchronizes the state with a new ticker snapshot.
    /// </summary>
    /// <param name="snapshot">The live price update snapshot.</param>
    public Task ReceivePriceUpdate(TickerSnapshot snapshot)
    {
        HandleBroadcasterUpdate(snapshot);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Synchronizes the state with the latest market metadata.
    /// </summary>
    /// <param name="markets">The complete list of supported trading pairs.</param>
    public Task ReceiveMarketMetadata(IReadOnlyList<MarketMetadata> markets)
    {
        OnMarketsUpdate?.Invoke(markets);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Establishes the initial state for the server-side container.
    /// </summary>
    public Task StartAsync(CancellationToken ct = default)
    {
        // No-op: The server-side state is directly tethered to the singleton broadcaster.
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _broadcaster.OnPriceUpdate -= HandleBroadcasterUpdate;
    }
}
