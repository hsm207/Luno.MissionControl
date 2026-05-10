namespace Luno.MissionControl.Application.Models;

/// <summary>
/// A lightweight summary of a placed order for UI consumption.
/// Crossing the architectural boundary as a DTO.
/// </summary>
/// <param name="OrderId">The unique identifier for the order.</param>
/// <param name="Pair">The trading pair for the order.</param>
public sealed record OrderSummaryDto(string OrderId, string Pair)
{
    public OrderSummaryDto() : this(string.Empty, string.Empty) { }
}
