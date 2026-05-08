
using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Application.Diagnostics;
using Luno.MissionControl.Core.Models;
using Luno.MissionControl.Core.Services;
using Luno.MissionControl.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace Luno.MissionControl.Application.UseCases;

/// <summary>
/// Orchestrates wallet-related use cases for the Wallets Hub.
/// </summary>
public class WalletOrchestrator(
    ILunoAccountAdapter accountAdapter,
    IWalletRepository walletRepository,
    WalletResolver resolver,
    ILogger<WalletOrchestrator> logger) : IWalletOrchestrator
{
    /// <summary>
    /// Fetches a high-level overview of all assets and their resolution status.
    /// </summary>
    public virtual async Task<List<Wallet>> GetWalletOverviewAsync(CancellationToken ct = default)
    {
        using var activity = ForensicTracing.StartActivity("GetWalletOverview");
        logger.LogInformation("Fetching wallet resolution overview...");

        var groupedAccounts = await accountAdapter.GetAccountsAsync(ct);
        var overview = new List<Wallet>();

        foreach (var (asset, assetAccounts) in groupedAccounts.OrderBy(x => x.Key))
        {
            var preference = await walletRepository.GetPreferenceAsync(asset, ct);

            long? resolvedId = null;
            try
            {
                var resolved = resolver.Resolve(assetAccounts, asset, preference, isBase: true);
                resolvedId = resolved.Id;
                ForensicMetrics.WalletsResolved.Add(1, new KeyValuePair<string, object?>("asset", asset));
            }
            catch (WalletAmbiguityException ex)
            {
                logger.LogWarning("Ambiguity detected for {Asset}: {Message}", asset, ex.Message);
                activity?.AddEvent(new("WalletAmbiguityDetected", DateTimeOffset.UtcNow, new() { { "asset", asset }, { "candidates", assetAccounts.Count } }));
                ForensicMetrics.WalletsAmbiguous.Add(1, new KeyValuePair<string, object?>("asset", asset));
            }
            catch (WalletNotFoundException ex)
            {
                logger.LogError("No accounts found for {Asset}: {Message}", asset, ex.Message);
                ForensicMetrics.WalletsNotFound.Add(1, new KeyValuePair<string, object?>("asset", asset));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during wallet resolution for {Asset}", asset);
            }

            overview.Add(new Wallet
            {
                Asset = asset,
                Accounts = assetAccounts,
                PinnedPreference = preference,
                ResolvedAccountId = resolvedId
            });
        }

        return overview;
    }

    /// <summary>
    /// Pins a specific account as the preferred trading wallet for its currency.
    /// </summary>
    public async Task PinAccountAsync(string asset, long accountId, CancellationToken ct = default)
    {
        using var activity = ForensicTracing.StartActivity("PinAccount");
        activity?.SetTag("asset", asset);
        activity?.SetTag("accountId", accountId);

        logger.LogInformation("Pinning account {AccountId} for asset {Asset}...", accountId, asset);

        // For now, we pin the same account for both Base and Counter roles if it's the same currency.
        // In a more complex setup, we might differentiate, but pinning one 'Trading Account' is the current goal.
        var preference = new TradingAccountPreference
        {
            CurrencyCode = asset,
            BaseAccountId = accountId,
            CounterAccountId = accountId,
            LastUpdated = DateTime.UtcNow
        };

        await walletRepository.SavePreferenceAsync(preference, ct);
        
        logger.LogInformation("Successfully pinned preference for {Asset}.", asset);
    }
}
