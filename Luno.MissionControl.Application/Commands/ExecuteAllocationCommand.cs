using Luno.MissionControl.Application.Models;

namespace Luno.MissionControl.Application.Commands;

/// <summary>
/// Represents a target allocation request within a command.
/// </summary>
public sealed record AllocationRequest(string Pair, decimal Weight);

/// <summary>
/// A command to execute a proportional multi-asset allocation based on a total spend.
/// The orchestrator is responsible for resolving the appropriate accounts based on user preferences.
/// </summary>
public sealed record ExecuteAllocationCommand(
    decimal TotalSpend,
    IReadOnlyList<AllocationRequest> Allocations);
