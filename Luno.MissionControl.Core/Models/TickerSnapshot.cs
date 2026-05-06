namespace Luno.MissionControl.Core.Models;

/// <summary>
/// Represents a point-in-time snapshot of market prices.
/// </summary>
public record TickerSnapshot(
    string MarketId, 
    decimal Ask, 
    decimal Bid, 
    decimal LastTrade, 
    long Timestamp);
