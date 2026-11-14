# Luno Exchange Migration

## Slice 3 — Refuse Unpinned Asset
- ✅ **Test 5 (Programmer Test):** Given an unpinned asset (`preference is null`) with a candidate `SPOT` account, When `ResolveSpotAccountId` runs, Then throw `UnpinnedWalletException` (`WalletResolverTests` → `WalletExceptions` + `WalletResolver.ResolveSpotAccountId`)
