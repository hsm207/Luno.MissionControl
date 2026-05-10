namespace Luno.MissionControl.Application.Models;

/// <summary>
/// A lightweight representation of market metadata for UI filtering.
/// Crossing the architectural boundary as a DTO.
/// </summary>
/// <param name="Pair">The trading pair identifier (e.g., "XBTZAR").</param>
/// <param name="BaseCurrency">The base currency (e.g., "XBT").</param>
/// <param name="CounterCurrency">The counter currency (e.g., "ZAR").</param>
public sealed record MarketMetadataDto(
    string Pair,
    string BaseCurrency,
    string CounterCurrency
);
