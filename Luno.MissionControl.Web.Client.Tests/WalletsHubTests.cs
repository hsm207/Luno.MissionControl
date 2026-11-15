using Bunit;
using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Core.Models;
using Luno.MissionControl.Web.Client.Components.Wallets;
using Luno.MissionControl.Web.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using NSubstitute;
using Xunit;

namespace Luno.MissionControl.Web.Client.Tests;

/// <summary>bUnit tests for <see cref="WalletsHub"/> rendering and SPOT account segregation.</summary>
public class WalletsHubTests : BunitContext
{
    private const int RenderTimeoutSeconds = 5;
    private const long PrimarySpotId = 101L;
    private const long SecondarySpotId = 102L;
    private const long TransactionalId = 201L;
    private const long StakingId = 301L;
    private const decimal DefaultSpotBalance = 1.0m;
    private const decimal DefaultNonSpotBalance = 0.5m;
    private const string SimulatedFailure = "Luno API unreachable";

    private static readonly TimeSpan RenderTimeout = TimeSpan.FromSeconds(RenderTimeoutSeconds);

    private readonly IWalletOrchestrator _mockOrchestrator;

    /// <summary>Initializes a fresh bUnit test context with mocked orchestrator and state bridge.</summary>
    public WalletsHubTests()
    {
        _mockOrchestrator = Substitute.For<IWalletOrchestrator>();
        Services.AddSingleton(_mockOrchestrator);
        Services.AddFluentUIComponents();
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddBunitPersistentComponentState();
        Services.AddScoped<IPersistenceBridge, PersistenceBridge>();
    }

    /// <summary>Given an unpinned SPOT asset, When rendered, Then displays Unpinned badge and Pin button.</summary>
    [Fact(DisplayName = "Given an unpinned SPOT asset, When rendered, "
        + "Then the Unpinned badge and Pin button must be visible")]
    public void GivenUnpinnedSpotAsset_WhenRendered_ThenUnpinnedBadgeVisible()
    {
        ArrangeWallet("XBT", null, Spot(PrimarySpotId, "Trading"), Spot(SecondarySpotId, "Savings"));

        var cut = Render<WalletsHub>();

        AssertMarkup(cut, expected: ["Unpinned", "Pin SPOT wallet"]);
    }

    /// <summary>Given a pinned SPOT asset, When rendered, Then displays Pinned badge and active styling.</summary>
    [Fact(DisplayName = "Given a pinned SPOT asset, When rendered, "
        + "Then the Pinned badge must be visible on the correct account")]
    public void GivenPinnedSpotAsset_WhenRendered_ThenPinnedBadgeVisible()
    {
        ArrangeWallet("MYR", PrimarySpotId, Spot(PrimarySpotId, "Main"));

        var cut = Render<WalletsHub>();

        AssertMarkup(cut, expected: ["Pinned", "is-active"]);
    }

    /// <summary>Given mixed account types, When rendered, Then only SPOT accounts expose Pin buttons.</summary>
    [Fact(DisplayName = "Given SPOT, TRANSACTIONAL, and STAKING accounts, "
        + "When rendered, Then only SPOT accounts expose Pin buttons")]
    public void GivenMixedAccountTypes_WhenRendered_ThenOnlySpotAccountsExposePinButtons()
    {
        ArrangeWallet("XBT", null, Spot(PrimarySpotId, "Spot"),
            NonSpot(TransactionalId, "TRANSACTIONAL", "Tx"), NonSpot(StakingId, "STAKING", "Stake"));

        var cut = Render<WalletsHub>();

        AssertMarkup(cut,
            expected: ["Spot Trading Wallets", PinButtonId(PrimarySpotId)],
            forbidden: [PinButtonId(TransactionalId), PinButtonId(StakingId)]);
    }

    /// <summary>Given load failure, When rendered, Then displays Error banner.</summary>
    [Fact(DisplayName = "Given load failure, When rendered, Then displays Error banner")]
    public void GivenOrchestratorLoadFailure_WhenRendered_ThenErrorBannerVisible()
    {
        ArrangeLoadFailure(SimulatedFailure);

        var cut = Render<WalletsHub>();

        AssertMarkup(cut, expected: ["Error", $"Failed to load wallets: {SimulatedFailure}"]);
    }

    /// <summary>Given pin failure, When Pin clicked, Then displays Error banner.</summary>
    [Fact(DisplayName = "Given pin failure, When Pin clicked, Then displays Error banner")]
    public void GivenOrchestratorPinFailure_WhenPinClicked_ThenErrorBannerVisible()
    {
        var cut = ArrangeRenderedHubWithFailingPin();

        cut.Find($"#{PinButtonId(PrimarySpotId)}").Click();

        AssertMarkup(cut, expected: ["Error", $"Failed to pin account: {SimulatedFailure}"]);
    }

    private static string PinButtonId(long accountId) => $"btn-{accountId}";

    private static LunoAccount Spot(long id, string name) =>
        new() { Id = id, Name = name, Balance = DefaultSpotBalance, AccountType = "SPOT" };

    private static LunoAccount NonSpot(long id, string accountType, string name) =>
        new() { Id = id, Name = name, Balance = DefaultNonSpotBalance, AccountType = accountType };

    private void ArrangeWallet(string asset, long? pinnedAccountId, params LunoAccount[] accounts)
    {
        TradingAccountPreference? preference = pinnedAccountId is { } id
            ? new TradingAccountPreference { CurrencyCode = asset, AccountId = id, LastUpdated = DateTime.UtcNow }
            : null;

        _mockOrchestrator.GetWalletOverviewAsync().Returns([
            new Wallet
            {
                Asset = asset,
                SpotAccounts = [.. accounts.Where(a => a.AccountType == "SPOT")],
                ViewOnlyAccounts = [.. accounts.Where(a => a.AccountType != "SPOT")],
                PinnedPreference = preference,
                ResolvedAccountId = pinnedAccountId
            }
        ]);
    }

    private void ArrangeLoadFailure(string message) =>
        _mockOrchestrator.GetWalletOverviewAsync()
            .Returns(Task.FromException<List<Wallet>>(new InvalidOperationException(message)));

    private IRenderedComponent<WalletsHub> ArrangeRenderedHubWithFailingPin()
    {
        ArrangeWallet("XBT", null, Spot(PrimarySpotId, "Trading"));
        _mockOrchestrator.PinAccountAsync("XBT", PrimarySpotId)
            .Returns(Task.FromException(new InvalidOperationException(SimulatedFailure)));
        var cut = Render<WalletsHub>();
        cut.WaitForElement($"#{PinButtonId(PrimarySpotId)}", RenderTimeout);
        return cut;
    }

    private static void AssertMarkup(
        IRenderedComponent<WalletsHub> cut,
        string[] expected,
        string[]? forbidden = null)
    {
        cut.WaitForAssertion(() =>
        {
            Assert.All(expected, text => Assert.Contains(text, cut.Markup));
            Assert.All(forbidden ?? [], text => Assert.DoesNotContain(text, cut.Markup));
        }, RenderTimeout);
    }
}
