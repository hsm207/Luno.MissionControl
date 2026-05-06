using Luno.MissionControl.Application.Models;

namespace Luno.MissionControl.Application.Commands;

/// <summary>
/// Represents a target allocation request within a command.
/// </summary>
public sealed record AllocationRequest(string Pair, decimal Weight);

/// <summary>
/// A command to execute a proportional multi-asset allocation based on a total spend.
/// </summary>
public sealed record ExecuteAllocationCommand(
    decimal TotalSpend,
    long BaseAccountId,
    long CounterAccountId,
    IReadOnlyList<AllocationRequest> Allocations);
