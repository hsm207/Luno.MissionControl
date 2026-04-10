namespace Luno.MissionControl.Application.Models;

/// <summary>
/// Represents a target allocation weight for a specific currency pair in a smart basket.
/// </summary>
/// <param name="Pair">The currency pair (e.g., XBTMYR).</param>
/// <param name="Weight">The proportional weight (0.0 to 1.0) of the total spend.</param>
public sealed record BasketAllocation(string Pair, decimal Weight);
