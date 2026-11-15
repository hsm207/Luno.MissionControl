using Luno.MissionControl.Application.UseCases;
using Luno.MissionControl.Core.Models;
using Luno.MissionControl.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Luno.MissionControl.Tests.Integration;

public class WalletOrchestratorTests
{
    private readonly StubAccountAdapter _accountAdapter = new();
    private readonly FakeWalletRepository _walletRepository = new();

    private WalletOrchestrator CreateOrchestrator() =>
        new(_accountAdapter, _walletRepository, new WalletResolver(), NullLogger<WalletOrchestrator>.Instance);

    [Fact(DisplayName = "Given unpinned SPOT and TRANSACTIONAL accounts, When GetWalletOverviewAsync runs, Then group accounts and leave ResolvedAccountId null")]
    public async Task GetWalletOverviewAsync_WhenUnpinned_GroupsSpotAndViewOnlyWithoutAutoSelect()
    {
        // --- 1. ARRANGE ---
        _accountAdapter.Accounts["XBT"] =
        [
            new() { Id = 101, Name = "XBT Spot", AccountType = "SPOT" },
            new() { Id = 102, Name = "XBT Transactional", AccountType = "TRANSACTIONAL" }
        ];
        var orchestrator = CreateOrchestrator();

        // --- 2. ACT ---
        var overview = await orchestrator.GetWalletOverviewAsync();

        // --- 3. ASSERT ---
        var wallet = Assert.Single(overview);
        Assert.Null(wallet.ResolvedAccountId);
        Assert.Equal(101, Assert.Single(wallet.SpotAccounts).Id);
        Assert.Equal(102, Assert.Single(wallet.ViewOnlyAccounts).Id);
    }

    [Fact(DisplayName = "Given a preference pinned to a TRANSACTIONAL account, When GetWalletOverviewAsync runs, Then leave ResolvedAccountId null")]
    public async Task GetWalletOverviewAsync_WhenPinnedToTransactional_LeavesResolvedAccountIdNull()
    {
        // --- 1. ARRANGE ---
        _accountAdapter.Accounts["XBT"] = [new() { Id = 102, Name = "XBT Transactional", AccountType = "TRANSACTIONAL" }];
        await _walletRepository.SavePreferenceAsync(new() { CurrencyCode = "XBT", AccountId = 102, LastUpdated = DateTime.UtcNow });
        var orchestrator = CreateOrchestrator();

        // --- 2. ACT ---
        var overview = await orchestrator.GetWalletOverviewAsync();

        // --- 3. ASSERT ---
        Assert.Null(Assert.Single(overview).ResolvedAccountId);
    }

    [Fact(DisplayName = "Given a preference pinned to a SPOT account, When GetWalletOverviewAsync runs, Then set ResolvedAccountId")]
    public async Task GetWalletOverviewAsync_WhenPinnedToSpot_SetsResolvedAccountId()
    {
        // --- 1. ARRANGE ---
        _accountAdapter.Accounts["XBT"] = [new() { Id = 101, Name = "XBT Spot", AccountType = "SPOT" }];
        await _walletRepository.SavePreferenceAsync(new() { CurrencyCode = "XBT", AccountId = 101, LastUpdated = DateTime.UtcNow });
        var orchestrator = CreateOrchestrator();

        // --- 2. ACT ---
        var overview = await orchestrator.GetWalletOverviewAsync();

        // --- 3. ASSERT ---
        Assert.Equal(101, Assert.Single(overview).ResolvedAccountId);
    }
}
