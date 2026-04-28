using Bunit;
using Luno.MissionControl.Application;
using Luno.MissionControl.Application.Models;
using Luno.MissionControl.Web.Client.Components.Dashboard;
using Luno.MissionControl.Web.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Moq;
using Xunit;

namespace Luno.MissionControl.Tests.Integration;

/// <summary>
/// High-Fidelity Tier 2 Verification of UI Behavior.
/// Uses bUnit to ensure the ReviewGate circuit breaker and Architect invariants are upheld.
/// </summary>
public class InteractionBehavioralTests : TestContext
{
    private readonly Mock<IBasketService> _mockBasketService;
    private readonly Mock<IBasketState> _mockBasketState;

    public InteractionBehavioralTests()
    {
        _mockBasketService = new Mock<IBasketService>();
        _mockBasketState = new Mock<IBasketState>();

        // Setup mock state to avoid SignalR dependencies in unit tests
        _mockBasketState.Setup(s => s.AvailableMarkets).Returns(new List<MarketMetadata>
        {
            new MarketMetadata("XBTZAR", "XBT", "ZAR"),
            new MarketMetadata("ETHZAR", "ETH", "ZAR")
        });
        _mockBasketState.Setup(s => s.Prices).Returns(new Dictionary<string, TickerSnapshot>());

        Services.AddFluentUIComponents();
        Services.AddLogging();
        Services.AddScoped(_ => _mockBasketService.Object);
        Services.AddScoped(_ => _mockBasketState.Object);
        
        // Mock the JS Interop for Fluent UI
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void ComboOrchestrator_InitialState_CalculatesCorrectSum()
    {
        // Act
        var cut = RenderComponent<ComboOrchestrator>();

        // Assert
        // We look for the label that displays the total allocation
        // Based on our default: XBT (0.6) + ETH (0.4) = 1.0 (100%)
        Assert.Contains("Allocation:", cut.Markup);
        Assert.Contains("100%", cut.Markup);
    }

    [Fact]
    public async Task ExecuteBasket_WhenWeightsDoNotSumTo100_ButtonIsDisabled()
    {
        // Arrange
        var cut = RenderComponent<ComboOrchestrator>();

        // Act: Update a weight to break the 100% sum (change XBT to 0.5)
        var weightInput = cut.FindComponent<WeightInput>();
        await cut.InvokeAsync(() => weightInput.Instance.WeightChanged.InvokeAsync(0.5m));

        // Assert
        var executeButton = cut.Find("fluent-button[appearance='primary']");
        Assert.NotNull(executeButton);
        Assert.True(executeButton.HasAttribute("disabled"), "Execute button should be disabled when total weight != 100%");
    }

    [Fact]
    public async Task ComboOrchestrator_AddAsset_UpdatesAllocationList()
    {
        // Arrange
        var cut = RenderComponent<ComboOrchestrator>();
        var initialButtons = cut.FindAll("fluent-button[appearance='subtle']").Count;
        Assert.Equal(2, initialButtons);
        
        // Select SOLMYR from the autocomplete
        var solMarket = new MarketMetadata("SOLMYR", "SOL", "MYR");
        var prop = cut.Instance.GetType().GetProperty("_selectedSearchItems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        prop!.SetValue(cut.Instance, new List<MarketMetadata> { solMarket });

        // Click the Add Asset button
        var addButton = cut.Find("fluent-button[appearance='outline']");
        addButton.Click();

        // Assert
        var buttonsAfterAdd = cut.FindAll("fluent-button[appearance='subtle']").Count;
        Assert.Equal(3, buttonsAfterAdd);
    }

    [Fact]
    public void ComboOrchestrator_RemoveAsset_UpdatesAllocationList()
    {
        // Arrange
        var cut = RenderComponent<ComboOrchestrator>();
        var initialButtons = cut.FindAll("fluent-button[appearance='subtle']").Count;
        Assert.Equal(2, initialButtons);

        // Act
        // The delete buttons are 'subtle' buttons.
        var deleteButtons = cut.FindAll("fluent-button[appearance='subtle']");
        
        // ETHMYR is the second default allocation
        deleteButtons[1].Click(); 

        // Assert
        var remainingButtons = cut.FindAll("fluent-button[appearance='subtle']").Count;
        Assert.Equal(1, remainingButtons);
    }
}
