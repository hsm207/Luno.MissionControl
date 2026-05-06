using System.Threading;
using System.Threading.Tasks;
using Luno.MissionControl.Core.Models;

namespace Luno.MissionControl.Application.Ports;

/// <summary>
/// Defines the contract for order execution and trading operations.
/// This interface decouples the application logic from the specific SDK transport.
/// </summary>
public interface ILunoTrader
{
    /// <summary>
    /// Calculates the estimated volume and price for an order given a spend amount.
    /// </summary>
    Task<OrderEstimation> EstimateOrderAsync(string pair, decimal spend, CancellationToken ct = default);

    /// <summary>
    /// Executes a limit order using the provided estimation.
    /// </summary>
    Task<string> PostOrderAsync(OrderEstimation estimation, long baseAccountId, long counterAccountId, CancellationToken ct = default);
}
