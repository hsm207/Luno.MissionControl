using Luno.MissionControl.Application.Models;
using Luno.SDK.Application.Trading;

namespace Luno.MissionControl.Application.Commands;

/// <summary>
/// A command to execute a proportional multi-asset allocation based on a total spend.
/// </summary>
/// <param name="TotalSpend">The total amount to spend in the default counter currency (e.g., MYR).</param>
/// <param name="Allocations">The list of target asset weights.</param>
public sealed record ExecuteAllocationCommand(
    decimal TotalSpend,
    IReadOnlyList<BasketAllocation> Allocations);

/// <summary>
/// Represents the result of a multi-asset basket execution.
/// </summary>
public sealed record BasketExecutionResult(
    bool Success,
    IReadOnlyList<OrderResponse> Orders,
    string? ErrorMessage = null);
