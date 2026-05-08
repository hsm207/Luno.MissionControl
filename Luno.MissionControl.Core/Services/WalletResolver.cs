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
    /// <param name="accounts">The list of all available Luno accounts.</param>
    /// <param name="targetCurrency">The currency code to resolve for (e.g., "XBT").</param>
    /// <param name="preference">Optional user-pinned preference for this currency.</param>
    /// <param name="isBase">Whether we are resolving for a Base currency or Counter currency (used to filter the preference).</param>
    /// <returns>The resolved LunoAccount.</returns>
    /// <exception cref="WalletAmbiguityException">Thrown when multiple accounts exist without a valid preference.</exception>
    /// <exception cref="WalletNotFoundException">Thrown when no accounts exist for the currency.</exception>
    public LunoAccount Resolve(
        IEnumerable<LunoAccount> accounts, 
        string targetCurrency, 
        TradingAccountPreference? preference,
        bool isBase = true)
    {
        var candidates = accounts
            .Where(a => a.Currency.Equals(targetCurrency, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count == 0)
        {
            throw new WalletNotFoundException(targetCurrency);
        }

        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        // Crisis Detected: Multiple accounts found. Check preference.
        if (preference is not null)
        {
            long preferredId = isBase ? preference.BaseAccountId : preference.CounterAccountId;
            var preferredAccount = candidates.FirstOrDefault(a => a.Id == preferredId);

            if (preferredAccount is not null)
            {
                return preferredAccount;
            }
        }

        // No preference or stale preference in a multi-account scenario.
        throw new WalletAmbiguityException(targetCurrency, candidates.Count);
    }
}
