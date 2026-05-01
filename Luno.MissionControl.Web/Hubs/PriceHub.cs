using Microsoft.AspNetCore.SignalR;
using Luno.MissionControl.Application;
using Luno.MissionControl.Web.Services;

namespace Luno.MissionControl.Web.Hubs;

/// <summary>
/// Manages real-time price snapshot broadcasting using SignalR.
/// Utilizes a strongly-typed <see cref="IPriceClient"/> to maintain architectural decoupling and type safety.
/// </summary>
public class PriceHub : Hub<IPriceClient>
{
    private readonly MarketInventory _inventory;

    public PriceHub(MarketInventory inventory)
    {
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
    }

    public override async Task OnConnectedAsync()
    {
        var markets = _inventory.GetMarkets();
        if (markets.Any())
        {
            await Clients.Caller.ReceiveMarketMetadata(markets);
        }
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Broadcasts a ticker snapshot to all connected clients.
    /// This method is primarily used for server-to-client streaming orchestrated by MarketWatchService.
    /// </summary>
    public async Task BroadcastPrice(TickerSnapshot snapshot)
    {
        await Clients.All.ReceivePriceUpdate(snapshot);
    }

    /// <summary>
    /// Broadcasts the full list of available markets to all connected clients.
    /// </summary>
    public async Task BroadcastMarketMetadata(IReadOnlyList<MarketMetadata> markets)
    {
        await Clients.All.ReceiveMarketMetadata(markets);
    }
}
