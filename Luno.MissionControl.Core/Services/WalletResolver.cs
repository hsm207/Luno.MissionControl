using Luno.MissionControl.Core.Exceptions;
using Luno.MissionControl.Core.Models;

namespace Luno.MissionControl.Core.Services;

/// <summary>
/// Domain service responsible for resolving the appropriate Luno account for a given currency 
/// from a set of candidates, respecting user preferences and enforcing the "Zero-Ambiguity" mandate.
/// </summary>
public class WalletResolver
{
    /// <summary>
    /// Resolves a single account from the available candidates.
    /// </summary>
    /// <param name="candidates">Available Luno accounts for the target currency.</param>
    /// <param name="targetCurrency">The currency code being resolved (for error reporting).</param>
    /// <param name="preference">Optional user preference for this currency.</param>
    /// <exception cref="WalletAmbiguityException">Thrown if multiple accounts exist without a preference.</exception>
    /// <exception cref="WalletNotFoundException">Thrown if no accounts exist.</exception>
    public LunoAccount Resolve(IEnumerable<LunoAccount> candidates, string targetCurrency, TradingAccountPreference? preference)
    {
        var matches = candidates.ToList();

        if (matches.Count == 0)
        {
            throw new WalletNotFoundException(targetCurrency);
        }

        if (matches.Count == 1)
        {
            return matches[0];
        }

        // Ambiguity Detected: Multiple accounts found. Check preference.
        if (preference is not null)
        {
            var preferred = matches.FirstOrDefault(a => a.Id == preference.AccountId);
            if (preferred is not null)
            {
                return preferred;
            }
        }

        // Crisis: Multiple accounts exist but no valid preference is set.
        throw new WalletAmbiguityException(targetCurrency, matches.Count);
    }
}

