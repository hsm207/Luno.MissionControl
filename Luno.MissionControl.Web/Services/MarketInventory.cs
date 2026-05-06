using Luno.MissionControl.Application;
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
    private readonly List<MarketMetadata> _markets = new();
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
