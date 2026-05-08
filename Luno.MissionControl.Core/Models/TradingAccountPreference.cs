namespace Luno.MissionControl.Core.Models;

/// <summary>
/// Represents a user's preferred trading accounts for a specific currency (e.g., MYR).
/// This is a pure POCO, decoupled from any database implementation.
/// </summary>
public class TradingAccountPreference
{
    /// <summary>
    /// Gets the currency code this preference applies to (e.g., "MYR").
    /// </summary>
    public string CurrencyCode { get; init; } = string.Empty;

    /// <summary>
    /// Gets the preferred Base Account ID (e.g., the XBT wallet).
    /// </summary>
    public long BaseAccountId { get; init; }

    /// <summary>
    /// Gets the preferred Counter Account ID (e.g., the MYR wallet).
    /// </summary>
    public long CounterAccountId { get; init; }

    /// <summary>
    /// Gets the timestamp when this preference was last updated.
    /// </summary>
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
}
