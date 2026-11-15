

namespace Luno.MissionControl.Core.Models;

/// <summary>
/// Represents the SPOT pinning status and account segregation for an asset wallet.
/// </summary>
public record Wallet
{
    /// <summary>
    /// Gets the currency code (e.g., "XBT").
    /// </summary>
    public string Asset { get; init; } = string.Empty;

    /// <summary>
    /// Gets the currently pinned preference for this asset, if any.
    /// </summary>
    public TradingAccountPreference? PinnedPreference { get; init; }

    /// <summary>
    /// Gets the ID of the active pinned SPOT account, if resolved.
    /// </summary>
    public long? ResolvedAccountId { get; init; }

    /// <summary>
    /// Gets the SPOT trading wallets for this asset (the only accounts eligible for pinning).
    /// </summary>
    public IReadOnlyList<LunoAccount> SpotAccounts { get; init; } = [];

    /// <summary>
    /// Gets non-SPOT accounts (e.g., TRANSACTIONAL, STAKING) displayed as view-only.
    /// </summary>
    public IReadOnlyList<LunoAccount> ViewOnlyAccounts { get; init; } = [];
}
