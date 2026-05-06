namespace Luno.MissionControl.Application.Models;

/// <summary>
/// A lightweight representation of RFC 7807 Problem Details for domain error propagation.
/// </summary>
public sealed record LunoProblemDetails(string? Title, string? Detail, int? Status);
