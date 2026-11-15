namespace Luno.MissionControl.Web.Client.Components.Wallets;

using Luno.MissionControl.Core.Models;

/// <summary>
/// Presentation projection of an asset's live SPOT accounts for <see cref="SpotWalletCard"/>.
/// </summary>
public sealed record SpotWalletViewModel
{
    public required string Asset { get; init; }
    public string? ResolvedAccountId { get; init; }
    public required IReadOnlyList<LunoAccount> SpotAccounts { get; init; }
}

/// <summary>
/// Presentation projection of an asset's non-SPOT accounts for view-only wallet tabs.
/// </summary>
public sealed record ViewOnlyWalletViewModel
{
    public required string Asset { get; init; }
    public required IReadOnlyList<LunoAccount> ViewOnlyAccounts { get; init; }
}
