using Luno.MissionControl.Application.Models;

namespace Luno.MissionControl.Application.Commands;

/// <summary>
/// A command to execute a proportional multi-asset allocation based on a total spend.
/// </summary>
/// <param name="TotalSpend">The total amount to spend in the default counter currency (e.g., MYR).</param>
/// <param name="Allocations">The list of target asset weights.</param>
public sealed record ExecuteAllocationCommand(
    decimal TotalSpend,
    IReadOnlyList<Allocation> Allocations);
