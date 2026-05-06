using Luno.MissionControl.Application.Models;
using Luno.MissionControl.Application;

namespace Luno.MissionControl.Application.Ports;

/// <summary>
/// An internal singleton service for broadcasting live price snapshots within the server process.
/// This allows Server-side Blazor components to receive updates without using a SignalR client loop.
/// </summary>
public interface IPriceBroadcaster
{
    /// <summary>
    /// Occurs when a new price snapshot is available.
    /// </summary>
    event Action<TickerSnapshot>? OnPriceUpdate;

    /// <summary>
    /// Broadcasts a snapshot to all local subscribers.
    /// </summary>
    void Broadcast(TickerSnapshot snapshot);
}

/// <summary>
/// A high-fidelity implementation of IPriceBroadcaster using a simple event-bus pattern.
/// </summary>
public class PriceBroadcaster : IPriceBroadcaster
{
    public event Action<TickerSnapshot>? OnPriceUpdate;

    public void Broadcast(TickerSnapshot snapshot)
    {
        OnPriceUpdate?.Invoke(snapshot);
    }
}
