using Luno.MissionControl.Application.Models;
using Luno.MissionControl.Application;

namespace Luno.MissionControl.Application.Ports;

/// <summary>
/// A high-fidelity interface for the dashboard's price state container.
/// Allows for platform-specific implementations (e.g. SignalR for WASM, direct for Server).
/// </summary>
public interface IBasketState : IPriceClient
{
    /// <summary>
    /// Occurs when a new price snapshot is available.
    /// </summary>
    event Action<TickerSnapshotDto>? OnPriceUpdate;

    /// <summary>
    /// Occurs when the inventory of available markets is updated.
    /// </summary>
    event Action<IReadOnlyList<MarketMetadataDto>>? OnMarketsUpdate;

    /// <summary>
    /// The current inventory of live prices.
    /// </summary>
    IReadOnlyDictionary<string, TickerSnapshotDto> Prices { get; }

    /// <summary>
    /// The full inventory of available markets from the Luno SDK.
    /// </summary>
    IReadOnlyList<MarketMetadataDto> AvailableMarkets { get; }

    /// <summary>
    /// The currently selected counter currency (e.g., "MYR", "USD").
    /// </summary>
    string SelectedCurrency { get; set; }

    /// <summary>
    /// The total amount of currency intended to be spent on the basket.
    /// </summary>
    decimal TargetSpend { get; set; }

    /// <summary>
    /// Starts the underlying connectivity (if any).
    /// </summary>
    Task StartAsync(CancellationToken ct = default);
}
