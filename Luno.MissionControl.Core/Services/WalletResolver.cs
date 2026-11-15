using Luno.MissionControl.Core.Exceptions;
using Luno.MissionControl.Core.Models;

namespace Luno.MissionControl.Core.Services;

/// <summary>
/// Domain service responsible for resolving a currency's pinned SPOT trading wallet
/// from live Luno accounts.
/// </summary>
public class WalletResolver
{
    /// <summary>
    /// Resolves the pinned <c>SPOT</c> account ID for a currency from the available candidates.
    /// </summary>
    /// <exception cref="UnpinnedWalletException">Thrown when no trading wallet preference is pinned.</exception>
    /// <exception cref="PinnedWalletNotFoundException">Thrown when the pinned account is absent.</exception>
    /// <exception cref="NonSpotWalletException">Thrown when the pinned account is not a SPOT wallet.</exception>
    public long ResolveSpotAccountId(
        IEnumerable<LunoAccount> candidates,
        string targetCurrency,
        TradingAccountPreference? preference)
    {
        if (preference is null)
        {
            throw new UnpinnedWalletException(targetCurrency);
        }

        var preferred = candidates.FirstOrDefault(a => a.Id == preference.AccountId);
        if (preferred is null)
        {
            throw new PinnedWalletNotFoundException(targetCurrency, preference.AccountId);
        }

        if (preferred.AccountType != "SPOT")
        {
            throw new NonSpotWalletException(targetCurrency, preferred.Id, preferred.AccountType);
        }

        return preferred.Id;
    }
}

