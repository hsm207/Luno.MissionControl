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
    private readonly Mock<IDialogService> _mockDialogService;

    public InteractionBehavioralTests()
    {
        _mockBasketService = new Mock<IBasketService>();
        _mockDialogService = new Mock<IDialogService>();

        Services.AddFluentUIComponents();
        Services.AddScoped(_ => _mockBasketService.Object);
        Services.AddScoped(_ => _mockDialogService.Object);
        Services.AddScoped<ClientBasketState>();
        Services.AddScoped<IBasketState>(sp => sp.GetRequiredService<ClientBasketState>());
        
        // Mock the JS Interop for Fluent UI
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void BasketArchitect_InitialState_CalculatesCorrectSum()
    {
        // Act
        var cut = RenderComponent<BasketArchitect>();

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
        var cut = RenderComponent<BasketArchitect>();

        // Act: Update a weight to break the 100% sum (change XBT to 0.5)
        var weightInput = cut.FindComponent<WeightInput>();
        await cut.InvokeAsync(() => weightInput.Instance.WeightChanged.InvokeAsync(0.5m));

        // Assert
        var executeButton = cut.Find("fluent-button[appearance='primary']");
        Assert.NotNull(executeButton);
        Assert.True(executeButton.HasAttribute("disabled"), "Execute button should be disabled when total weight != 100%");
    }

    [Fact]
    public async Task BasketArchitect_AddAsset_UpdatesAllocationList()
    {
        // Arrange
        var cut = RenderComponent<BasketArchitect>();
        var initialButtons = cut.FindAll("fluent-button[appearance='subtle']").Count;
        Assert.Equal(2, initialButtons);
        
        // Select SOLMYR from the autocomplete
        var autocomplete = cut.FindComponent<FluentAutocomplete<MarketMetadata, string>>();
        // We'll simulate a selection by setting SelectedItems directly
        var solMarket = new MarketMetadata("SOLMYR", "SOL", "MYR");
        await cut.InvokeAsync(() => autocomplete.Instance.SelectedItems = new List<MarketMetadata> { solMarket });

        // Click the Add Asset button
        var addButton = cut.Find("fluent-button[appearance='outline']");
        addButton.Click();

        // Assert
        var buttonsAfterAdd = cut.FindAll("fluent-button[appearance='subtle']").Count;
        Assert.Equal(3, buttonsAfterAdd);
    }

    [Fact]
    public void BasketArchitect_RemoveAsset_UpdatesAllocationList()
    {
        // Arrange
        var cut = RenderComponent<BasketArchitect>();
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
