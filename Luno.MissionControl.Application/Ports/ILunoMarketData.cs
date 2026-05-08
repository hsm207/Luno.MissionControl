using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreModels = Luno.MissionControl.Core.Models;

namespace Luno.MissionControl.Application.Ports;

/// <summary>
/// Defines the contract for market metadata and account state discovery.
/// </summary>
public interface ILunoMarketData
{

    /// <summary>
    /// Retrieves metadata for a set of trading pairs.
    /// </summary>
    Task<IReadOnlyList<CoreModels.MarketMetadata>> GetMarketsAsync(IEnumerable<string> pairs, CancellationToken ct = default);
}
