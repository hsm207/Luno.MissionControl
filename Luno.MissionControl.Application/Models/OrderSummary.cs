namespace Luno.MissionControl.Application.Models;

/// <summary>
/// A lightweight summary of a placed order for UI consumption without SDK dependencies.
/// </summary>
public sealed record OrderSummary(string OrderId, string Pair);
