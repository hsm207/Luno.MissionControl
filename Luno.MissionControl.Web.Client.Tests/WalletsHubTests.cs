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

public class WalletsHubTests : BunitContext
{
    private readonly IWalletOrchestrator _mockOrchestrator;

    public WalletsHubTests()
    {
        _mockOrchestrator = Substitute.For<IWalletOrchestrator>();
        Services.AddSingleton(_mockOrchestrator);
        Services.AddFluentUIComponents();
        JSInterop.Mode = JSRuntimeMode.Loose;
        
        // Register the real PersistenceBridge using the bUnit-provided fake PersistentComponentState
        this.AddBunitPersistentComponentState();
        Services.AddScoped<IPersistenceBridge, PersistenceBridge>();
    }

    [Fact(DisplayName = "Given an ambiguous asset, When rendered, Then the ambiguity indicator must be visible")]
    public void GivenAmbiguousAsset_WhenRendered_ThenAmbiguityIndicatorVisible()
    {
        // Arrange
        _mockOrchestrator.GetWalletOverviewAsync().Returns(new List<Wallet>
        {
            new()
            {
                Asset = "XBT",
                Accounts = [
                    new() { Id = 1, Name = "Trading", Balance = 1.0m },
                    new() { Id = 2, Name = "Savings", Balance = 0.5m }
                ],
                PinnedPreference = null,
                ResolvedAccountId = null
            }
        });

        // Act
        var cut = Render<WalletsHub>();

        // Assert
        cut.WaitForAssertion(() => 
        {
            Assert.Contains("Needs attention", cut.Markup);
        }, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "Given a pinned asset, When rendered, Then the active badge must be visible on the correct account")]
    public void GivenPinnedAsset_WhenRendered_ThenActiveBadgeVisible()
    {
        // Arrange
        _mockOrchestrator.GetWalletOverviewAsync().Returns(new List<Wallet>
        {
            new()
            {
                Asset = "MYR",
                Accounts = [
                    new() { Id = 10, Name = "Main", Balance = 1000m }
                ],
                PinnedPreference = new TradingAccountPreference { CurrencyCode = "MYR", BaseAccountId = 10, CounterAccountId = 10 },
                ResolvedAccountId = 10
            }
        });

        // Act
        var cut = Render<WalletsHub>();

        // Assert
        cut.WaitForAssertion(() => 
        {
            Assert.Contains("Ready", cut.Markup);
            Assert.Contains("is-active", cut.Markup);
        }, TimeSpan.FromSeconds(5));
    }
}
