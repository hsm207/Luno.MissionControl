namespace Luno.MissionControl.Application.Models;

/// <summary>
/// Represents the result of a basket execution operation.
/// </summary>
/// <param name="Success">Indicates if the entire sequence succeeded.</param>
/// <param name="Orders">The list of order summaries received.</param>
/// <param name="ErrorMessage">Optional error details if failed.</param>
public sealed record BasketExecutionResponse(
    bool Success,
    IReadOnlyList<OrderSummary> Orders,
    string? ErrorMessage = null);
