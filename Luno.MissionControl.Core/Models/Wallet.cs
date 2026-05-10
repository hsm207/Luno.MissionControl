

namespace Luno.MissionControl.Core.Models;

/// <summary>
/// Represents the resolution status of an asset-based wallet.
/// </summary>
public record Wallet
{
    /// <summary>
    /// Gets the currency code (e.g., "XBT").
    /// </summary>
    public string Asset { get; init; } = string.Empty;

    /// <summary>
    /// Gets the list of available accounts for this asset.
    /// </summary>
    public List<LunoAccount> Accounts { get; init; } = [];

    /// <summary>
    /// Gets the currently pinned preference for this asset, if any.
    /// </summary>
    public TradingAccountPreference? PinnedPreference { get; init; }

    /// <summary>
    /// Gets whether this asset has multiple accounts without a pinned preference.
    /// </summary>
    public bool IsAmbiguous => Accounts.Count > 1 && PinnedPreference == null;

    /// <summary>
    /// Gets the ID of the 'Active' account, if resolution is possible.
    /// </summary>
    public long? ResolvedAccountId { get; init; }
}
