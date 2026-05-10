using Luno.MissionControl.Core.Models;

namespace Luno.MissionControl.Application.Ports;

/// <summary>
/// An Input Port defining the contract for wallet orchestration.
/// This allows for both server-side and client-side (WASM) implementations 
/// in InteractiveAuto mode.
/// </summary>
public interface IWalletOrchestrator
{
    /// <summary>
    /// Fetches a high-level overview of all assets and their resolution status.
    /// </summary>
    Task<List<Wallet>> GetWalletOverviewAsync(CancellationToken ct = default);

    /// <summary>
    /// Pins a specific account as the preferred trading wallet for its currency.
    /// </summary>
    Task PinAccountAsync(string asset, long accountId, CancellationToken ct = default);
}
