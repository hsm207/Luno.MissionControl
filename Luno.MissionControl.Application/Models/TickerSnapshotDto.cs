namespace Luno.MissionControl.Application.Models;

/// <summary>
/// A lightweight price snapshot for UI consumption.
/// Crossing the architectural boundary as a DTO.
/// </summary>
/// <param name="Pair">The trading pair identifier.</param>
/// <param name="Price">The current market price (last trade).</param>
/// <param name="Ask">The current lowest sell price.</param>
/// <param name="Bid">The current highest buy price.</param>
/// <param name="Timestamp">The UTC timestamp of the snapshot.</param>
public sealed record TickerSnapshotDto(
    string Pair,
    decimal Price,
    decimal Ask,
    decimal Bid,
    DateTimeOffset Timestamp
);
