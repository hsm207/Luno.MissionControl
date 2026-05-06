namespace Luno.MissionControl.Application.Models;

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
