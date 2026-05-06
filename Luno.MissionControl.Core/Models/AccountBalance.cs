namespace Luno.MissionControl.Core.Models;

/// <summary>
/// Domain model for an account balance.
/// </summary>
public record AccountBalance(string Asset, decimal Available, string AccountId);
