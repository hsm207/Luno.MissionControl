using Luno.MissionControl.Application;
using Luno.MissionControl.Application.Models;
using System.Collections.Generic;

namespace Luno.MissionControl.Web.Services;

/// <summary>
/// A singleton service that maintains the current inventory of available markets.
/// This allows SignalR hubs to provide the initial state to joining clients.
/// </summary>
public class MarketInventory
{
    private static readonly IReadOnlyList<MarketMetadata> BootstrapMarkets = 
    [
        new("XBTMYR", "XBT", "MYR"),
        new("ETHMYR", "ETH", "MYR"),
        new("XBTUSDC", "XBT", "USDC"),
        new("ETHUSDC", "ETH", "USDC")
    ];

    private readonly List<MarketMetadata> _markets = [.. BootstrapMarkets];
    private readonly object _lock = new();

    public void UpdateMarkets(IEnumerable<MarketMetadata> markets)
    {
        lock (_lock)
        {
            _markets.Clear();
            _markets.AddRange(markets);
        }
    }

    public IReadOnlyList<MarketMetadata> GetMarkets()
    {
        lock (_lock)
        {
            return _markets.ToArray();
        }
    }
}
