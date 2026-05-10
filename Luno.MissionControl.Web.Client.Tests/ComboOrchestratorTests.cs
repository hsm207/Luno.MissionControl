using Bunit;
using Luno.MissionControl.Application;
using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Application.Models;
using Luno.MissionControl.Web.Client.Components.Dashboard;
using Luno.MissionControl.Web.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using NSubstitute;
using Xunit;

namespace Luno.MissionControl.Web.Client.Tests;

public class ComboOrchestratorTests : BunitContext
{
    [Fact]
    public void WeightInputs_ShouldHaveUniqueIds()
    {
        // Arrange
        var mockState = Substitute.For<IBasketState>();
        var mockBasketService = Substitute.For<IBasketService>();
        var mockLogger = Substitute.For<ILogger<ComboOrchestrator>>();

        // Setup mock data for initial state
        mockState.AvailableMarkets.Returns(new List<MarketMetadataDto>
        {
            new("XBTMYR", "XBT", "MYR"),
            new("ETHMYR", "ETH", "MYR")
        });
        mockState.SelectedCurrency.Returns("MYR");
        mockState.TargetSpend.Returns(1000m);

        // Register custom mocks
        Services.AddSingleton(mockState);
        Services.AddSingleton(mockBasketService);
        Services.AddSingleton(mockLogger);

        // Register Fluent UI services for component rendering
        Services.AddFluentUIComponents();

        // Configure loose JSInterop for headless rendering
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Register PersistentComponentState and Bridge to satisfy component dependencies
        this.AddBunitPersistentComponentState();
        Services.AddScoped<IPersistenceBridge, PersistenceBridge>();

        // Act
        var cut = Render<ComboOrchestrator>();

        // Wait for the FluentDataGrid to finish its async binding/virtualization lifecycle
        var weightInputs = cut.WaitForComponents<WeightInput>();

        // Assert: Each WeightInput should have a unique, deterministic ID
        var xbtInput = weightInputs.FirstOrDefault(c => c.Instance.Id == "weight-input-XBTMYR");
        var ethInput = weightInputs.FirstOrDefault(c => c.Instance.Id == "weight-input-ETHMYR");

        Assert.NotNull(xbtInput);
        Assert.NotNull(ethInput);
        Assert.NotEqual(xbtInput.Instance.Id, ethInput.Instance.Id);
    }
}

