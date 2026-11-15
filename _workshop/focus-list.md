# Luno Exchange Migration

## Slice 6 — Strangler Cutover: `BasketOrchestrator` Pre-Trade Guard
- ✅ **Test 9 (Application Test):** Given a 2-allocation basket (`ETHMYR` pinned `SPOT`, `XRPMYR` pinned `TRANSACTIONAL`), When `ExecuteAsync` runs, Then return failure and place **zero** orders (`SpyTrader.Calls` is empty) (`WalletResolutionIntegrationTests` → `BasketOrchestrator.ExecuteAsync`)
- ✅ **Test 10 (Application Test):** Given unpinned single/multiple accounts vs pinned `SPOT` accounts, When `ExecuteAsync` runs, Then refuse unpinned and execute only pinned `SPOT` accounts (`WalletResolutionIntegrationTests`)
