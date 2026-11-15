# Luno Exchange Migration

## Slice 7 — Strangler Cutover: `WalletOrchestrator` & Overview Projection
- ✅ **Test 11 (Application Test):** Given unpinned `SPOT` + `TRANSACTIONAL` accounts, When `GetWalletOverviewAsync` runs, Then `ResolvedAccountId` is `null` and accounts are grouped into `SpotAccounts` vs `ViewOnlyAccounts` (`WalletOrchestratorTests` → `Wallet.cs` + `WalletOrchestrator.cs`)
- ✅ **Test 12 (Application Test):** Given a preference pinned to a `TRANSACTIONAL` account, When `GetWalletOverviewAsync` runs, Then `ResolvedAccountId` is `null` (`WalletOrchestratorTests`)
- ✅ **Test 13 (Application Test):** Given a preference pinned to a `SPOT` account, When `GetWalletOverviewAsync` runs, Then `ResolvedAccountId` equals the pinned `SPOT` `AccountId` (`WalletOrchestratorTests`)
- ✅ **Refinement:** Expose `SpotAccounts` and `ViewOnlyAccounts` on `WalletOverviewViewModel` and `WalletsHub.razor.cs` (`WalletOverviewViewModel.cs`, `WalletsHub.razor.cs`)
