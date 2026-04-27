namespace Luno.MissionControl.Application.Models;

/// <summary>
/// Represents a target allocation weight for a specific currency pair in a smart basket.
/// </summary>
/// <param name="Pair">The currency pair (e.g., XBTMYR).</param>
/// <param name="Weight">The proportional weight (0.0 to 1.0) of the total spend.</param>
public sealed record Allocation(string Pair, decimal Weight);

/// <summary>
/// A request to execute a full basket allocation.
/// </summary>
/// <param name="TotalSpend">The total amount of counter currency to spend.</param>
/// <param name="Allocations">The list of pair weights.</param>
public sealed record BasketExecutionRequest(decimal TotalSpend, IReadOnlyList<Allocation> Allocations);

/// <summary>
/// A lightweight summary of a placed order for UI consumption without SDK dependencies.
/// </summary>
/// <param name="OrderId">The remote order ID.</param>
/// <param name="Pair">The market pair.</param>
public sealed record OrderSummary(string OrderId, string Pair);

/// <summary>
/// Represents the result of a multi-asset basket execution.
/// </summary>
/// <param name="Success">Indicates if the entire sequence succeeded.</param>
/// <param name="Orders">The list of order summaries received.</param>
/// <param name="ErrorMessage">Optional error details if failed.</param>
public sealed record BasketExecutionResult(
    bool Success,
    IReadOnlyList<OrderSummary> Orders,
    string? ErrorMessage = null);

/// <summary>
/// A lightweight representation of RFC 7807 Problem Details for domain error propagation.
/// </summary>
public sealed record LunoProblemDetails(string? Title, string? Detail, int? Status);
