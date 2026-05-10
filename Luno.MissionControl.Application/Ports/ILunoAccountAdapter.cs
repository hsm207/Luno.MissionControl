using Luno.MissionControl.Core.Models;

namespace Luno.MissionControl.Application.Ports;

/// <summary>
/// Port for fetching live account data from Luno.
/// This interface is defined by the Application layer to satisfy its own requirements.
/// </summary>
public interface ILunoAccountAdapter
{
    /// <summary>
    /// Fetches all available Luno accounts for the authenticated user.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A dictionary of LunoAccount models, grouped by asset currency code.</returns>
    Task<IDictionary<string, List<LunoAccount>>> GetAccountsAsync(CancellationToken ct = default);
}
