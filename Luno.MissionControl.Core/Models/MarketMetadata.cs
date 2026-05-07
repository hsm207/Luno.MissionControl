namespace Luno.MissionControl.Core.Models;

/// <summary>
/// Represents the fundamental metadata for a trading pair.
/// </summary>
public record MarketMetadata(
    string MarketId,
    string BaseCurrency,
    string CounterCurrency,
    int PriceScale,
    int AmountScale,
    decimal MinAmount,
    decimal MinPrice);
