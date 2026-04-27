namespace Luno.MissionControl.Application;

/// <summary>
/// Defines the SignalR client contract for receiving real-time price updates.
/// This interface is used by the PriceHub to broadcast strongly-typed messages.
/// </summary>
public interface IPriceClient
{
    /// <summary>
    /// Receives a new price snapshot from the server.
    /// </summary>
    Task ReceivePriceUpdate(TickerSnapshot snapshot);

    /// <summary>
    /// Receives the full list of available markets from the server.
    /// This is typically called once on connection or when the market list changes.
    /// </summary>
    Task ReceiveMarketMetadata(IReadOnlyList<MarketMetadata> markets);
}

/// <summary>
/// A lightweight representation of market metadata for UI filtering.
/// </summary>
public record MarketMetadata(
    string Pair,
    string BaseCurrency,
    string CounterCurrency
);

/// <summary>
/// A lightweight snapshot for UI consumption.
/// </summary>
public record TickerSnapshot(
    string Pair,
    decimal Price,
    decimal Ask,
    decimal Bid,
    DateTimeOffset Timestamp
);
