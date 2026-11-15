# Luno Exchange Migration

## Slice 5 — Remaining Non-`SPOT` Types & Missing Pin
- ✅ **Test 7 (Programmer Test):** Given a pinned non-`SPOT` account (`"STAKING"`, `"SAVINGS"`, `"EARN"`, `""`), When `ResolveSpotAccountId` runs, Then throw `NonSpotWalletException` (`WalletResolverTests`)
- ✅ **Test 8 (Programmer Test):** Given a pinned `AccountId` absent from live candidates, When `ResolveSpotAccountId` runs, Then throw `PinnedWalletNotFoundException` (`WalletResolverTests` → `WalletExceptions` + `WalletResolver.ResolveSpotAccountId`)
