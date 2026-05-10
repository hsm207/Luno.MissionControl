using Luno.MissionControl.Application.Commands;
using Luno.MissionControl.Application.Models;

namespace Luno.MissionControl.Application.Ports;

/// <summary>
/// Defines the stable contract for basket-related operations across the UI and BFF.
/// </summary>
public interface IBasketService
{
    /// <summary>
    /// Executes a full basket allocation based on the provided command.
    /// </summary>
    Task<BasketExecutionResponseDto> ExecuteAsync(ExecuteAllocationCommand command, CancellationToken ct = default);
}
