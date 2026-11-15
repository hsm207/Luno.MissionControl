# Luno Exchange Migration

## Slice 4 — First Mechanism: Reject `"TRANSACTIONAL"` Pin
- ✅ **Test 6 (Programmer Test):** Given a pinned `"TRANSACTIONAL"` account, When `ResolveSpotAccountId` runs, Then throw `NonSpotWalletException` (`WalletResolverTests` → `WalletExceptions` + `WalletResolver.ResolveSpotAccountId`)
