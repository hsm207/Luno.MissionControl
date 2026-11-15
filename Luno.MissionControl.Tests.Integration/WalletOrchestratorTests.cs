using Luno.MissionControl.Application.UseCases;
using Luno.MissionControl.Core.Models;
using Luno.MissionControl.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Luno.MissionControl.Tests.Integration;

/// <summary>Integration tests for <see cref="WalletOrchestrator"/> SPOT overview resolution.</summary>
public class WalletOrchestratorTests
{
    private const long SpotAccountId = 101L;
    private const long TransactionalAccountId = 102L;

    private readonly StubAccountAdapter _accountAdapter = new();
    private readonly FakeWalletRepository _walletRepository = new();

    private WalletOrchestrator CreateOrchestrator() =>
        new(_accountAdapter, _walletRepository, new WalletResolver(), NullLogger<WalletOrchestrator>.Instance);

    /// <summary>Given unpinned SPOT and TRANSACTIONAL accounts, Groups them and leaves ResolvedAccountId null.</summary>
    [Fact(DisplayName = "Given unpinned SPOT and TRANSACTIONAL accounts, "
        + "When GetWalletOverviewAsync runs, Then group accounts and leave ResolvedAccountId null")]
    public async Task GetWalletOverviewAsync_WhenUnpinned_GroupsSpotAndViewOnlyWithoutAutoSelect()
    {
        _accountAdapter.Accounts["XBT"] =
        [
            new() { Id = SpotAccountId, Name = "XBT Spot", AccountType = "SPOT" },
            new() { Id = TransactionalAccountId, Name = "XBT Transactional", AccountType = "TRANSACTIONAL" }
        ];
        var orchestrator = CreateOrchestrator();

        var overview = await orchestrator.GetWalletOverviewAsync();

        var wallet = Assert.Single(overview);
        Assert.Null(wallet.ResolvedAccountId);
        Assert.Equal(SpotAccountId, Assert.Single(wallet.SpotAccounts).Id);
        Assert.Equal(TransactionalAccountId, Assert.Single(wallet.ViewOnlyAccounts).Id);
    }

    /// <summary>Given a preference pinned to TRANSACTIONAL, Leaves ResolvedAccountId null.</summary>
    [Fact(DisplayName = "Given a preference pinned to a TRANSACTIONAL account, "
        + "When GetWalletOverviewAsync runs, Then leave ResolvedAccountId null")]
    public async Task GetWalletOverviewAsync_WhenPinnedToTransactional_LeavesResolvedAccountIdNull()
    {
        _accountAdapter.Accounts["XBT"] =
            [new() { Id = TransactionalAccountId, Name = "XBT Transactional", AccountType = "TRANSACTIONAL" }];
        await SavePreferenceAsync("XBT", TransactionalAccountId);
        var orchestrator = CreateOrchestrator();

        var overview = await orchestrator.GetWalletOverviewAsync();

        Assert.Null(Assert.Single(overview).ResolvedAccountId);
    }

    /// <summary>Given a preference pinned to SPOT, Sets ResolvedAccountId.</summary>
    [Fact(DisplayName = "Given a preference pinned to a SPOT account, "
        + "When GetWalletOverviewAsync runs, Then set ResolvedAccountId")]
    public async Task GetWalletOverviewAsync_WhenPinnedToSpot_SetsResolvedAccountId()
    {
        _accountAdapter.Accounts["XBT"] = [new() { Id = SpotAccountId, Name = "XBT Spot", AccountType = "SPOT" }];
        await SavePreferenceAsync("XBT", SpotAccountId);
        var orchestrator = CreateOrchestrator();

        var overview = await orchestrator.GetWalletOverviewAsync();

        Assert.Equal(SpotAccountId, Assert.Single(overview).ResolvedAccountId);
    }

    private Task SavePreferenceAsync(string asset, long accountId) =>
        _walletRepository.SavePreferenceAsync(
            new() { CurrencyCode = asset, AccountId = accountId, LastUpdated = DateTime.UtcNow });
}
