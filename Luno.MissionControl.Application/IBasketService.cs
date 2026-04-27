namespace Luno.MissionControl.Application;

using Luno.MissionControl.Application.Models;

/// <summary>
/// Defines the stable contract for basket-related operations across the UI and BFF.
/// </summary>
public interface IBasketService
{
    /// <summary>
    /// Executes a basket allocation across multiple markets.
    /// </summary>
    Task<BasketExecutionResult> ExecuteAsync(BasketExecutionRequest request, CancellationToken ct = default);
}
