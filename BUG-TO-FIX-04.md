# BUG-04: Server-Side Metadata Blindness

## Status
- **Priority**: High (Breaks basic UI functionality during initial load)
- **Phase**: Diagnostic Confirmed / Planning Fix

## Context
When running in **InteractiveServer** mode (initial load or incognito before WASM hydration), the `ComboOrchestrator` relies on `ServerBasketState` for its data orchestration.

## The Problem
The `ServerBasketState` implementation is "blind" to market metadata, leading to a breakdown in UI reactivity during the server-side phase of the `InteractiveAuto` lifecycle.

1. **Empty Inventory**: `ServerBasketState.AvailableMarkets` is hardcoded to return `Array.Empty<MarketMetadata>()`.
2. **Missing Wiring**: While `MarketWatchService` updates the singleton `MarketInventory`, `ServerBasketState` does not consume this inventory nor does it receive the SignalR broadcasts intended for WASM clients.

## Symptoms
- **Allocation Vanishing**: Switching the target currency (e.g., USDC -> MYR) causes the basket to empty ("No data to show"). This happens because `TransitionCurrency` fails to find equivalent markets in the empty `AvailableMarkets` list.
- **Search Failure**: The "Search for a coin" input returns zero results, preventing users from adding assets during the server-side phase.

## Root Cause
In [ServerBasketState.cs](file:///home/user/Documents/GitHub/Luno.MissionControl/Luno.MissionControl.Web/Services/ServerBasketState.cs#L19):
```csharp
public IReadOnlyList<MarketMetadata> AvailableMarkets => Array.Empty<MarketMetadata>();
```

## Proposed Fix
1. **Inject Inventory**: Inject the `MarketInventory` singleton into `ServerBasketState`.
2. **Delegate Lookup**: Update `AvailableMarkets` to return `_marketInventory.GetMarkets()`.
3. **Internal Subscription**: Ensure `ServerBasketState` provides a consistent view of markets to the `ComboOrchestrator` even before the first price update.

## Verification Plan
1. Open a fresh incognito browser.
2. Verify the "Search for a coin" input displays a list of available assets.
3. Verify that switching the investment currency preserves the existing basket allocations.
