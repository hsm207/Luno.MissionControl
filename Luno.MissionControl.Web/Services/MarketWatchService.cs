using Luno.MissionControl.Application;
using Luno.MissionControl.Application.Models;
using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Luno.SDK;
using Luno.SDK.Application.Market;
using System.Diagnostics.Metrics;

namespace Luno.MissionControl.Web.Services;

/// <summary>
/// A background worker that polls the Luno SDK for market snapshots every 60 seconds
/// and broadcasts them to all connected clients via the PriceHub SignalR bridge.
/// </summary>
public class MarketWatchService : BackgroundService
{
    private static readonly Meter s_meter = new("Luno.MissionControl.Web.MarketWatch");
    private readonly ILunoClient _lunoClient;
    private readonly IPriceBroadcaster _broadcaster;
    private readonly MarketInventory _inventory;
    private readonly IHubContext<PriceHub, IPriceClient> _hubContext;
    private readonly ILogger<MarketWatchService> _logger;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(30));
    private int _heartbeatValue = 1;

    public MarketWatchService(
        ILunoClient lunoClient,
        IPriceBroadcaster broadcaster,
        MarketInventory inventory,
        IHubContext<PriceHub, IPriceClient> hubContext,
        ILogger<MarketWatchService> logger)
    {
        _lunoClient = lunoClient ?? throw new ArgumentNullException(nameof(lunoClient));
        _broadcaster = broadcaster ?? throw new ArgumentNullException(nameof(broadcaster));
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Register a heartbeat gauge to replace the noisy "I'm alive" log spam
        s_meter.CreateObservableGauge("market_watch_heartbeat", () => _heartbeatValue);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("MarketWatchService starting with 30s heartbeat (Economy Mode).");

            // Fetch and broadcast market metadata once on startup
            try
            {
                await BroadcastMarketMetadataAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Initial market metadata fetch failed.");
            }

            // Initial sweep on startup to provide immediate telemetry
            // We wrap this in a silent try-catch to prevent a transient API failure from killing the Host
            try
            {
                await BroadcastTickersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Initial market telemetry sweep failed. Dashboard will update on next heartbeat.");
            }

            int tickCount = 0;
            while (await _timer.WaitForNextTickAsync(stoppingToken))
            {
                _logger.LogDebug("Streaming market snapshots at {Time}", DateTimeOffset.UtcNow);

                // Refresh metadata every 10 ticks (approx every 5 minutes)
                if (++tickCount % 10 == 0)
                {
                    await BroadcastMarketMetadataAsync(stoppingToken);
                }

                await BroadcastTickersAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MarketWatchService is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MarketWatchService encountered a fatal unhandled exception.");
        }
    }

    private async Task BroadcastMarketMetadataAsync(CancellationToken ct)
    {
        _logger.LogInformation("Updating market metadata inventory...");
        var markets = await _lunoClient.Market.GetMarketsAsync(new GetMarketsQuery(), ct);

        var metadata = markets.Select(m => new MarketMetadataDto(
            Pair: m.Pair,
            BaseCurrency: m.BaseCurrency,
            CounterCurrency: m.CounterCurrency
        )).ToList();

        // Update the singleton inventory for new connections
        _inventory.UpdateMarkets(metadata);

        // Broadcast via the strongly-typed hub bridge
        await _hubContext.Clients.All.ReceiveMarketMetadata(metadata);
        _logger.LogInformation("Broadcasted {Count} market definitions to all clients.", metadata.Count);
    }

    private async Task BroadcastTickersAsync(CancellationToken ct)
    {
        await foreach (var ticker in _lunoClient.Market.GetTickersAsync(new GetTickersQuery(), ct))
        {
            if (ticker == null) continue;

            // Mapping Phase 3: TickerResponse (SDK) -> TickerSnapshotDto (BFF)
            var snapshot = new TickerSnapshotDto(
                Pair: ticker.Pair,
                Price: ticker.Price,
                Ask: ticker.Ask,
                Bid: ticker.Bid,
                Timestamp: ticker.Timestamp
            );

            // BroadCast Phase: Hybrid Delivery
            // 1. SignalR (for WASM / Out-of-process)
            await _hubContext.Clients.All.ReceivePriceUpdate(snapshot);

            // 2. Direct (for Server / In-process)
            _broadcaster.Broadcast(snapshot);
        }
    }
}
