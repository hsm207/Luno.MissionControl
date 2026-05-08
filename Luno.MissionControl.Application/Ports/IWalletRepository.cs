using Luno.MissionControl.Core.Models;

namespace Luno.MissionControl.Application.Ports;

/// <summary>
/// Port for persisting and retrieving user wallet preferences. 🫦
/// </summary>
public interface IWalletRepository
{
    /// <summary>
    /// Retrieves the preferred account ID for the specified key.
    /// </summary>
    Task<TradingAccountPreference?> GetPreferenceAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Persists the user's preference for a specific account.
    /// </summary>
    Task SavePreferenceAsync(TradingAccountPreference preference, CancellationToken ct = default);
}
