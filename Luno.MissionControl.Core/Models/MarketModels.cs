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

/// <summary>
/// Represents a point-in-time snapshot of market prices.
/// </summary>
public record TickerSnapshot(
    string MarketId, 
    decimal Ask, 
    decimal Bid, 
    decimal LastTrade, 
    long Timestamp);
