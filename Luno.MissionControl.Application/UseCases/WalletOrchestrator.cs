using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Application.Diagnostics;
using Luno.MissionControl.Core.Models;
using Luno.MissionControl.Core.Services;
using Luno.MissionControl.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace Luno.MissionControl.Application.UseCases;

/// <summary>
/// Orchestrates the resolution and persistence of wallet preferences, ensuring deterministic 
/// account selection for multi-asset trading workflows.
/// </summary>
public class WalletOrchestrator(
    ILunoAccountAdapter accountAdapter,
    IWalletRepository walletRepository,
    WalletResolver resolver,
    ILogger<WalletOrchestrator> logger) : IWalletOrchestrator
{
    /// <summary>
    /// Resolves the current state of all wallets, identifying which account is currently pinned/resolved for each asset.
    /// </summary>
    public virtual async Task<List<Wallet>> GetWalletOverviewAsync(CancellationToken ct = default)
    {
        using var activity = ForensicTracing.StartActivity("GetWalletOverview");
        logger.LogInformation("Fetching wallet resolution overview...");

        var groupedAccounts = await accountAdapter.GetAccountsAsync(ct);
        var overview = new List<Wallet>();

        foreach (var (asset, assetAccounts) in groupedAccounts.OrderBy(x => x.Key))
        {
            overview.Add(await BuildWalletOverviewItemAsync(asset, assetAccounts, ct));
        }

        return overview;
    }

    private async Task<Wallet> BuildWalletOverviewItemAsync(
        string asset,
        List<LunoAccount> assetAccounts,
        CancellationToken ct)
    {
        var preference = await walletRepository.GetPreferenceAsync(asset, ct);
        var resolvedId = ResolveSpotIdForOverview(asset, assetAccounts, preference);

        return new Wallet
        {
            Asset = asset,
            Accounts = assetAccounts,
            PinnedPreference = preference,
            ResolvedAccountId = resolvedId
        };
    }

    private long? ResolveSpotIdForOverview(
        string asset,
        List<LunoAccount> assetAccounts,
        TradingAccountPreference? preference)
    {
        try
        {
            var resolvedId = resolver.ResolveSpotAccountId(assetAccounts, asset, preference);
            ForensicMetrics.WalletsResolved.Add(1, new KeyValuePair<string, object?>("asset", asset));
            return resolvedId;
        }
        catch (UnpinnedWalletException ex)
        {
            logger.LogInformation("Asset {Asset} has no pinned SPOT wallet yet: {Message}", asset, ex.Message);
            return null;
        }
        catch (NonSpotWalletException ex)
        {
            logger.LogWarning("Pinned account for {Asset} is not a SPOT wallet: {Message}", asset, ex.Message);
            return null;
        }
        catch (PinnedWalletNotFoundException ex)
        {
            logger.LogWarning("Pinned account for {Asset} missing from live balances: {Message}", asset, ex.Message);
            ForensicMetrics.WalletsNotFound.Add(1, new KeyValuePair<string, object?>("asset", asset));
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during wallet resolution for {Asset}", asset);
            return null;
        }
    }

    /// <summary>
    /// Pins a specific account as the preferred trading wallet for its currency.
    /// </summary>
    public async Task PinAccountAsync(string asset, long accountId, CancellationToken ct = default)
    {
        using var activity = ForensicTracing.StartActivity("PinAccount");
        activity?.SetTag("asset", asset);
        activity?.SetTag("account.id", accountId);

        logger.LogInformation("Pinning account {AccountId} for asset {Asset}...", accountId, asset);

        // For now, we pin the same account for both roles to maintain zero-ambiguity.
        // Pinning one 'Trading Account' is the current goal.
        // HARDENED PINNING: Only one AccountId per currency
        var preference = new TradingAccountPreference
        {
            CurrencyCode = asset,
            AccountId = accountId,
            LastUpdated = DateTime.UtcNow
        };

        await walletRepository.SavePreferenceAsync(preference, ct);

        logger.LogInformation("Successfully pinned preference for {Asset}.", asset);
    }
}
