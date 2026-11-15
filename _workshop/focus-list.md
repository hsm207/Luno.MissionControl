# Luno Exchange Migration

## Slice 8 — Strangler Cutover: Don Norman `WalletsHub` Tabs
- ✅ **Test 14 (UI Test):** Given an asset with `SPOT`, `TRANSACTIONAL`, and `STAKING` accounts, When `WalletsHub` renders, Then only the `SPOT` account has a Pin button (`btn-{id}`) and `TRANSACTIONAL`/`STAKING` tabs have zero Pin affordances (`WalletsHubTests` → `WalletsHub.razor`)
- ✅ **Test 15 (UI Test):** Given unpinned vs pinned `SPOT` wallets, When `WalletsHub` renders, Then display `Unpinned` / `Pinned` state badges (`WalletsHubTests` → `WalletsHub.razor.cs`)
- ✅ **Test 16 (UI Error Path Tests):** Given orchestrator load or pin failure, When `WalletsHub` loads or handles a Pin click, Then render the `Error` message bar (`WalletsHubTests` → `WalletsHub.razor.cs`)
