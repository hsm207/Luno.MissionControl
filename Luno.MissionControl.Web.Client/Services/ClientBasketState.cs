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
    private readonly ConcurrentDictionary<string, TickerSnapshotDto> _prices = new();
    private readonly List<MarketMetadataDto> _markets = new();

    public event Action<TickerSnapshotDto>? OnPriceUpdate;
    public event Action<IReadOnlyList<MarketMetadataDto>>? OnMarketsUpdate;

    public IReadOnlyDictionary<string, TickerSnapshotDto> Prices => _prices;
    public IReadOnlyList<MarketMetadataDto> AvailableMarkets => _markets;

    public string SelectedCurrency { get; set; } = "MYR";
    public decimal TargetSpend { get; set; } = 1000m;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientBasketState"/> class.
    /// </summary>
    /// <param name="hubConnection">The SignalR connection to the PriceHub.</param>
    /// <param name="logger">The diagnostic logger instance.</param>
    public ClientBasketState(
        [FromKeyedServices("PriceHub")] HubConnection hubConnection,
        ILogger<ClientBasketState> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));

        _hubConnection.On<TickerSnapshotDto>(nameof(ReceivePriceUpdate), ReceivePriceUpdate);
        _hubConnection.On<IReadOnlyList<MarketMetadataDto>>(nameof(ReceiveMarketMetadata), ReceiveMarketMetadata);
    }

    /// <summary>
    /// Processes an incoming ticker snapshot from the SignalR stream and broadcasts the update.
    /// </summary>
    /// <param name="snapshot">The live price update snapshot.</param>
    public Task ReceivePriceUpdate(TickerSnapshotDto snapshot)
    {
        _logger.LogTrace("Price Received: {Pair} = {Price}", snapshot.Pair, snapshot.Price);
        _prices[snapshot.Pair] = snapshot;
        OnPriceUpdate?.Invoke(snapshot);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Processes a full market metadata update, typically received upon initial connection or configuration changes.
    /// </summary>
    /// <param name="markets">The complete list of supported trading pairs.</param>
    public Task ReceiveMarketMetadata(IReadOnlyList<MarketMetadataDto> markets)
    {
        _logger.LogDebug("Received metadata for {Count} trading pairs.", markets.Count);
        _markets.Clear();
        _markets.AddRange(markets);
        OnMarketsUpdate?.Invoke(_markets);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Establishes the real-time bridge connection to the SignalR PriceHub.
    /// </summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            await _hubConnection.StartAsync(ct);
        }
    }

    /// <summary>
    /// Terminates the bridge connection to the SignalR PriceHub.
    /// </summary>
    public Task StopAsync(CancellationToken ct = default) => _hubConnection.StopAsync(ct);
}
