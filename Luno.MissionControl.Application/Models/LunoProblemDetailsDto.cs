namespace Luno.MissionControl.Application.Models;

/// <summary>
/// A standardized representation of an application-level failure crossing the architectural boundary.
/// Conforms to the RFC 7807 Problem Details for HTTP APIs.
/// </summary>
public sealed record LunoProblemDetailsDto(string? Title, string? Detail, int? Status);
