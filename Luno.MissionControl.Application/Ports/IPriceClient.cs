using Luno.MissionControl.Application.Models;
namespace Luno.MissionControl.Application.Ports;

/// <summary>
/// Defines the SignalR client contract for receiving real-time price updates.
/// This interface is used by the PriceHub to broadcast strongly-typed messages.
/// </summary>
public interface IPriceClient
{
    /// <summary>
    /// Receives a new price snapshot from the server.
    /// </summary>
    Task ReceivePriceUpdate(TickerSnapshotDto snapshot);

    /// <summary>
    /// Receives the full list of available markets from the server.
    /// This is typically called once on connection or when the market list changes.
    /// </summary>
    Task ReceiveMarketMetadata(IReadOnlyList<MarketMetadataDto> markets);
}
