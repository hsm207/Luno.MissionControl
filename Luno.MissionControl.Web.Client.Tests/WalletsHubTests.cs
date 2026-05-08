using Bunit;

using Luno.MissionControl.Application.UseCases;
using Luno.MissionControl.Core.Models;
using Luno.MissionControl.Web.Client.Components.Wallets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using NSubstitute;
using Xunit;

namespace Luno.MissionControl.Web.Client.Tests;

public class WalletsHubTests : BunitContext
{
    [Fact(DisplayName = "Given an ambiguous asset, When rendered, Then the ambiguity indicator must be visible")]
    public async Task GivenAmbiguousAsset_WhenRendered_ThenAmbiguityIndicatorVisible()
    {
        // Arrange
        var mockOrchestrator = Substitute.For<WalletOrchestrator>(
            Substitute.For<Luno.MissionControl.Application.Ports.ILunoAccountAdapter>(),
            Substitute.For<Luno.MissionControl.Application.Ports.IWalletRepository>(),
            new Luno.MissionControl.Core.Services.WalletResolver(),
            Substitute.For<Microsoft.Extensions.Logging.ILogger<WalletOrchestrator>>()
        );

        mockOrchestrator.GetWalletOverviewAsync().Returns(new List<Wallet>
        {
            new()
            {
                Asset = "XBT",
                Accounts = new List<LunoAccount>
                {
                    new() { Id = 1, Name = "Trading", Balance = 1.0m },
                    new() { Id = 2, Name = "Savings", Balance = 0.5m }
                },
                PinnedPreference = null,
                ResolvedAccountId = null
            }
        });

        Services.AddSingleton(mockOrchestrator);
        Services.AddFluentUIComponents();
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Act
        var cut = Render<WalletsHub>();

        // Assert
        cut.WaitForAssertion(() => 
        {
            Assert.Contains("Needs attention", cut.Markup);
            Assert.Contains("chip-needs-attention", cut.Markup);
        }, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "Given a pinned asset, When rendered, Then the active badge must be visible on the correct account")]
    public async Task GivenPinnedAsset_WhenRendered_ThenActiveBadgeVisible()
    {
        // Arrange
        var mockOrchestrator = Substitute.For<WalletOrchestrator>(
            Substitute.For<Luno.MissionControl.Application.Ports.ILunoAccountAdapter>(),
            Substitute.For<Luno.MissionControl.Application.Ports.IWalletRepository>(),
            new Luno.MissionControl.Core.Services.WalletResolver(),
            Substitute.For<Microsoft.Extensions.Logging.ILogger<WalletOrchestrator>>()
        );

        mockOrchestrator.GetWalletOverviewAsync().Returns(new List<Wallet>
        {
            new()
            {
                Asset = "MYR",
                Accounts = new List<LunoAccount>
                {
                    new() { Id = 10, Name = "Main", Balance = 1000m }
                },
                PinnedPreference = new TradingAccountPreference { CurrencyCode = "MYR", BaseAccountId = 10, CounterAccountId = 10 },
                ResolvedAccountId = 10
            }
        });

        Services.AddSingleton(mockOrchestrator);
        Services.AddFluentUIComponents();
        JSInterop.Mode = JSRuntimeMode.Loose;

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
