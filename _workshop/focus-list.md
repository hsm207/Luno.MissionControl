# Luno Exchange Migration

## Slice 2 — Expand `ResolveSpotAccountId` Happy Path
- ✅ **Test 4 (Programmer Test):** Given a pinned `TradingAccountPreference` matching a `SPOT` account among candidates, When `ResolveSpotAccountId` runs, Then return the pinned `SPOT` `AccountId` (`WalletResolverTests` → `WalletResolver.ResolveSpotAccountId`)
