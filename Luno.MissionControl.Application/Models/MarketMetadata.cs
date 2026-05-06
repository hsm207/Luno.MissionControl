namespace Luno.MissionControl.Application.Models;

/// <summary>
/// A lightweight representation of market metadata for UI filtering.
/// </summary>
public record MarketMetadata(
    string Pair,
    string BaseCurrency,
    string CounterCurrency
);
