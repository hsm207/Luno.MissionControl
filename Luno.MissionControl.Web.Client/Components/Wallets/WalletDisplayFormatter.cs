using Luno.MissionControl.Core.Models;

namespace Luno.MissionControl.Web.Client.Components.Wallets;

/// <summary>
/// Pre-formatted account row model consumed by <see cref="WalletCard"/>.
/// </summary>
public sealed record WalletAccountRowModel
{
    public required long AccountId { get; init; }
    public required string DisplayName { get; init; }
    public required string MaskedId { get; init; }
    public required string FormattedBalance { get; init; }
    public bool IsActive { get; init; }
    public string? SectionLabel { get; init; }
}

/// <summary>
/// Pre-projected asset card model consumed by <see cref="WalletCard"/>.
/// </summary>
public sealed record WalletCardModel
{
    public required string Asset { get; init; }
    public required IReadOnlyList<WalletAccountRowModel> Accounts { get; init; }
    public string StateClass { get; init; } = "auto-selected";
    public string StateLabel { get; init; } = "View-Only";
    public string? UrgencyHint { get; init; }
}

/// <summary>
/// Pure single-pass builders and formatting helpers for wallet cards.
/// </summary>
internal static class WalletDisplayFormatter
{
    public static WalletCardModel BuildSpotCard(SpotWalletViewModel wallet)
    {
        bool isPinned = !string.IsNullOrEmpty(wallet.ResolvedAccountId);
        var sorted = wallet.SpotAccounts
            .OrderByDescending(a => IsActive(wallet.ResolvedAccountId, a))
            .ToList();

        return new WalletCardModel
        {
            Asset = wallet.Asset,
            Accounts = BuildSpotRows(wallet, sorted),
            StateClass = isPinned ? "active-selected" : "needs-attention",
            StateLabel = isPinned ? "Pinned" : "Unpinned",
            UrgencyHint = isPinned ? null : "Pin a SPOT wallet to enable trading."
        };
    }

    public static IReadOnlyList<WalletCardModel> BuildViewOnlyCards(
        IEnumerable<ViewOnlyWalletViewModel> wallets,
        Func<LunoAccount, bool> predicate,
        string? fallbackLabel = null)
    {
        var cards = new List<WalletCardModel>();
        foreach (var wallet in wallets)
        {
            var matching = wallet.ViewOnlyAccounts.Where(predicate).ToList();
            if (matching.Count == 0)
            {
                continue;
            }

            var rows = matching
                .Select(a => CreateViewOnlyRow(a, fallbackLabel ?? a.AccountType))
                .ToList();
            cards.Add(new WalletCardModel { Asset = wallet.Asset, Accounts = rows });
        }

        return cards;
    }

    private static IReadOnlyList<WalletAccountRowModel> BuildSpotRows(
        SpotWalletViewModel wallet,
        IReadOnlyList<LunoAccount> sorted)
    {
        bool isPinned = !string.IsNullOrEmpty(wallet.ResolvedAccountId);
        long? firstInactiveId = isPinned && sorted.Count > 1
            ? sorted.FirstOrDefault(a => !IsActive(wallet.ResolvedAccountId, a))?.Id
            : null;

        var rows = new List<WalletAccountRowModel>(sorted.Count);
        foreach (var account in sorted)
        {
            rows.Add(CreateSpotRow(wallet, account, firstInactiveId));
        }

        return rows;
    }

    private static WalletAccountRowModel CreateSpotRow(
        SpotWalletViewModel wallet,
        LunoAccount account,
        long? firstInactiveId) =>
        new()
        {
            AccountId = account.Id,
            DisplayName = FormatAccountName(account, $"Unnamed {wallet.Asset} account"),
            MaskedId = MaskAccountId(account.Id),
            FormattedBalance = FormatBalance(account.Balance),
            IsActive = IsActive(wallet.ResolvedAccountId, account),
            SectionLabel = GetSpotSectionLabel(wallet.ResolvedAccountId, account, firstInactiveId)
        };

    private static WalletAccountRowModel CreateViewOnlyRow(
        LunoAccount account,
        string fallback) =>
        new()
        {
            AccountId = account.Id,
            DisplayName = FormatAccountName(account, fallback),
            MaskedId = MaskAccountId(account.Id),
            FormattedBalance = FormatBalance(account.Balance),
            IsActive = false,
            SectionLabel = null
        };

    private static string? GetSpotSectionLabel(
        string? resolvedAccountId,
        LunoAccount account,
        long? firstInactiveId)
    {
        if (string.IsNullOrEmpty(resolvedAccountId))
        {
            return null;
        }

        if (resolvedAccountId == account.Id.ToString())
        {
            return "Active SPOT wallet";
        }

        return account.Id == firstInactiveId ? "Other SPOT wallet" : null;
    }

    private static bool IsActive(string? resolvedAccountId, LunoAccount account) =>
        resolvedAccountId == account.Id.ToString();

    private static string FormatAccountName(LunoAccount account, string fallback) =>
        string.IsNullOrWhiteSpace(account.Name) ? fallback : account.Name;

    private static string MaskAccountId(long accountId)
    {
        var raw = accountId.ToString();
        return raw.Length > 5 ? raw.Substring(raw.Length - 5) : raw;
    }

    private static string FormatBalance(decimal balance) =>
        balance.ToString("N8").TrimEnd('0').TrimEnd('.');
}
