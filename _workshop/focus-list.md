# Luno Exchange Migration

## Slice 9 — Contract: Strangle Legacy Code & Prune Tests Under Green
- ✅ Remove strangled legacy domain/application symbols (`Wallet.Accounts`, `Wallet.IsAmbiguous`, `WalletResolver.Resolve`, `WalletAmbiguityException`, `WalletNotFoundException`, `ForensicMetrics.WalletsAmbiguous`) and populate `SpotAccounts`/`ViewOnlyAccounts` directly (`Wallet.cs`, `WalletOrchestrator.cs`)
- ✅ Prune superseded legacy auto-select/ambiguity tests and update callers (`WalletResolverTests.cs`, `WalletPersistenceIntegrationTests.cs`, `WalletsHubTests.cs`) under 100% green bar
