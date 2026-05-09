# Hardening Wallet Resolution Logic

This plan addresses the architectural mismatch between our currency preferences and trade roles by simplifying the preference model and introducing rigorous integration testing.

## User Review Required

> [!IMPORTANT]
> **Breaking Schema Change**: We are migrating `TradingAccountPreference` from a dual-role model (`BaseAccountId`/`CounterAccountId`) to a single `AccountId` model. This assumes that a user's preference for a currency is global, regardless of its role in a specific trading pair.

> [!WARNING]
> **Database Migration**: This change will require an EF Core migration to update the `AccountPreferences` table.

## Proposed Changes

### Core Layer

#### [MODIFY] [TradingAccountPreference.cs](file:///home/user/Documents/GitHub/Luno.MissionControl/Luno.MissionControl.Core/Models/TradingAccountPreference.cs)
- Remove `BaseAccountId` and `CounterAccountId`.
- Add a single `long AccountId` property.

#### [MODIFY] [WalletResolver.cs](file:///home/user/Documents/GitHub/Luno.MissionControl/Luno.MissionControl.Core/Services/WalletResolver.cs)
- Update `Resolve` signature to remove `bool isBase`:
  `public LunoAccount Resolve(IEnumerable<LunoAccount> candidates, string targetCurrency, TradingAccountPreference? preference)`
- Update logic to always use the single `preference.AccountId` if a preference exists.

### Application Layer

#### [MODIFY] [WalletOrchestrator.cs](file:///home/user/Documents/GitHub/Luno.MissionControl/Luno.MissionControl.Application/UseCases/WalletOrchestrator.cs)
- Update `PinAccountAsync` to set the new `AccountId` field.

#### [MODIFY] [BasketOrchestrator.cs](file:///home/user/Documents/GitHub/Luno.MissionControl/Luno.MissionControl.Application/UseCases/BasketOrchestrator.cs)
- Update calls to `resolver.Resolve` by removing the `isBase` argument. 
  Example: `resolver.Resolve(baseCandidates ?? [], market.BaseCurrency, basePreference)`

### Infrastructure Layer

#### [MODIFY] [SettingsDbContext.cs](file:///home/user/Documents/GitHub/Luno.MissionControl/Luno.MissionControl.Infrastructure/Persistence/SettingsDbContext.cs)
- Update the EF Core configuration for `TradingAccountPreference`.

### Testing

#### [NEW] [ResolutionHardeningTests](file:///home/user/Documents/GitHub/Luno.MissionControl/labs/ResolutionHardeningTests)
- Create a new .NET 10 XUnit project in the `labs/` directory to demonstrate modern, high-performance integration testing.
- **Composition Root**: Use `ServiceCollection` to wire up the real `BasketOrchestrator` and `WalletResolver`.
- **Spy Boundary**: Implement a `SpyTrader : ILunoTrader` that captures the `baseAccountId` and `counterAccountId` passed to `PostOrderAsync`.
- **Scenario**: A user has 2 ETH accounts and 1 MYR account, with a preference pinned to the second ETH account.
- **Assertion**: Verify the `SpyTrader` recorded the correct IDs for the `ETH/MYR` trade.

## Verification Plan

### Automated Tests
- Run `dotnet test` on the new `labs/ResolutionHardeningTests` project.
- Verify that `BasketOrchestrator` correctly maps preferences to the base/counter slots.

### Manual Verification
- Launch the app and verify the "Wallets Hub" correctly reflects the "Active" status of a pinned account.
- Verify that switching an account updates the DB correctly via the logs.
