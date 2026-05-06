using Microsoft.AspNetCore.SignalR.Client;
using Luno.MissionControl.Application.Models;
using Luno.MissionControl.Application;
using Luno.MissionControl.Application.Ports;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Luno.MissionControl.Web.Client.Services;

/// <summary>
/// A high-fidelity reactive state container for the Mission Control dashboard.
/// Subscribes to the SignalR PriceHub and manages the live price inventory via the IBasketState contract.
/// </summary>
public class ClientBasketState : IBasketState
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<ClientBasketState> _logger;
    private readonly ConcurrentDictionary<string, TickerSnapshot> _prices = new();
    private readonly List<MarketMetadata> _markets = new();
    
    public event Action<TickerSnapshot>? OnPriceUpdate;
    public event Action<IReadOnlyList<MarketMetadata>>? OnMarketsUpdate;

    public IReadOnlyDictionary<string, TickerSnapshot> Prices => _prices;
    public IReadOnlyList<MarketMetadata> AvailableMarkets => _markets;

    public string SelectedCurrency { get; set; } = "MYR";
    public decimal TargetSpend { get; set; } = 1000m;
    public long BaseAccountId { get; set; }
    public long CounterAccountId { get; set; }

    public ClientBasketState(
        [FromKeyedServices("PriceHub")] HubConnection hubConnection, 
        ILogger<ClientBasketState> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));

        // Registration using the contract's method name to ensure Phase 3 alignment
        _hubConnection.On<TickerSnapshot>(nameof(ReceivePriceUpdate), ReceivePriceUpdate);
        _hubConnection.On<IReadOnlyList<MarketMetadata>>(nameof(ReceiveMarketMetadata), ReceiveMarketMetadata);
    }

    /// <summary>
    /// Explicit implementation of the IPriceClient contract.
    /// Updates the local inventory and notifies UI subscribers.
    /// </summary>
    public Task ReceivePriceUpdate(TickerSnapshot snapshot)
    {
        _logger.LogTrace("Price Received: {Pair} = {Price}", snapshot.Pair, snapshot.Price);
        _prices[snapshot.Pair] = snapshot;
        OnPriceUpdate?.Invoke(snapshot);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Explicit implementation of the IPriceClient contract for market metadata.
    /// </summary>
    public Task ReceiveMarketMetadata(IReadOnlyList<MarketMetadata> markets)
    {
        _logger.LogDebug("Received metadata for {Count} trading pairs.", markets.Count);
        _markets.Clear();
        _markets.AddRange(markets);
        OnMarketsUpdate?.Invoke(_markets);
        return Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            await _hubConnection.StartAsync(ct);
        }
    }

    public Task StopAsync(CancellationToken ct = default) => _hubConnection.StopAsync(ct);
}
