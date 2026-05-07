using Bunit;
using Luno.MissionControl.Web.Client.Components.Dashboard;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.AspNetCore.Components;
using Xunit;
using System.Globalization;

namespace Luno.MissionControl.Web.Client.Tests;

public class WeightInputTests : BunitContext
{
    public WeightInputTests()
    {
        // Register Fluent UI services
        Services.AddFluentUIComponents();
        
        // Configure loose JSInterop for headless rendering
        JSInterop.Mode = JSRuntimeMode.Loose;
        
        // Register PersistentComponentState to satisfy component dependencies
        this.AddBunitPersistentComponentState();
    }

    [Fact(DisplayName = "WeightInput should accept integer inputs and propagate them as percentage decimals")]
    public async Task WeightInput_ShouldAcceptInteger_AndPropagatePercent()
    {
        // Arrange
        decimal? result = null;
        var cut = Render<WeightInput>(parameters => parameters
            .Add(p => p.Id, "test-weight-input")
            .Add(p => p.Weight, 0m)
            .Add(p => p.WeightChanged, EventCallback.Factory.Create<decimal>(this, val => result = val))
        );

        // Act - Simulate user typing "10" into the input
        cut.Find("#test-weight-input").Change("10");

        // Assert
        Assert.Equal(0.1m, result);
    }

    [Fact(DisplayName = "WeightInput should accept inputs with up to two decimal places")]
    public async Task WeightInput_ShouldAcceptTwoDecimals_AndPropagatePercent()
    {
        // Arrange
        decimal? result = null;
        var cut = Render<WeightInput>(parameters => parameters
            .Add(p => p.Id, "test-weight-input")
            .Add(p => p.Weight, 0m)
            .Add(p => p.WeightChanged, EventCallback.Factory.Create<decimal>(this, val => result = val))
        );

        // Act - Simulate user typing "10.52" into the input
        cut.Find("#test-weight-input").Change("10.52");

        // Assert
        Assert.Equal(0.1052m, result);
    }

    [Fact(DisplayName = "WeightInput should show a validation error when input precision exceeds two decimal places")]
    public void WeightInput_ShouldShowError_WhenMoreThanTwoDecimals()
    {
        // Arrange
        var cut = Render<WeightInput>(parameters => parameters
            .Add(p => p.Id, "test-weight-input")
            .Add(p => p.Weight, 0m)
        );

        // Act - Simulate user typing "10.523"
        cut.Find("#test-weight-input").Change("10.523");

        // Assert - Verify error message is rendered
        var errorMsg = cut.Find(".validation-error");
        Assert.Equal("Only 2 decimal places allowed!", errorMsg.TextContent);
        System.Console.WriteLine("[DIAGNOSTIC] Test: Validation message found: " + errorMsg.TextContent);
    }
}
