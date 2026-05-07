namespace Luno.MissionControl.Core.Models;

/// <summary>
/// Represents a validated estimation for an order execution.
/// </summary>
public record OrderEstimation(
    string Pair,
    decimal Volume,
    decimal Price,
    decimal TotalSpend);
