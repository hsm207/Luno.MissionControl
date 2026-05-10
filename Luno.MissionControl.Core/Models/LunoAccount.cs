namespace Luno.MissionControl.Core.Models;

/// <summary>
/// Represents a pure domain-level Luno account.
/// This model is strictly decoupled from the Luno SDK and persistence layers.
/// </summary>
public record LunoAccount
{
    /// <summary>
    /// Gets the unique identifier for the account (cryptographic identity).
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// Gets the human-friendly name of the account (e.g., "XBT Wallet").
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the current available balance in this account.
    /// </summary>
    public decimal Balance { get; init; }
}
