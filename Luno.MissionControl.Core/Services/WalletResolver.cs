using Luno.MissionControl.Core.Exceptions;
using Luno.MissionControl.Core.Models;

namespace Luno.MissionControl.Core.Services;

/// <summary>
/// Domain service for deterministic Luno account resolution.
/// Enforces the 'No More Guessing' mandate by failing-fast on any detected ambiguity.
/// </summary>
public class WalletResolver
{
    /// <summary>
    /// Resolves a single account for the given currency from a list of available accounts.
    /// </summary>
    /// <param name="candidates">The pre-filtered list of available accounts for the target asset.</param>
    /// <param name="targetCurrency">The currency code (used for exception reporting only).</param>
    /// <param name="preference">Optional user-pinned preference for this currency.</param>
    /// <param name="isBase">Whether we are resolving for a Base currency or Counter currency.</param>
    /// <returns>The resolved LunoAccount.</returns>
    public LunoAccount Resolve(
        IEnumerable<LunoAccount> candidates, 
        string targetCurrency,
        TradingAccountPreference? preference,
        bool isBase = true)
    {
        var candidateList = candidates.ToList();

        if (candidateList.Count == 0)
        {
            throw new WalletNotFoundException(targetCurrency);
        }

        if (candidateList.Count == 1)
        {
            return candidateList[0];
        }

        // Ambiguity Detected: Multiple accounts found. Check preference.
        if (preference is not null)
        {
            long preferredId = isBase ? preference.BaseAccountId : preference.CounterAccountId;
            var preferredAccount = candidateList.FirstOrDefault(a => a.Id == preferredId);

            if (preferredAccount is not null)
            {
                return preferredAccount;
            }
        }

        // No preference or stale preference in a multi-account scenario.
        throw new WalletAmbiguityException(targetCurrency, candidateList.Count);
    }
}
