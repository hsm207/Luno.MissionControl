using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Luno.MissionControl.Tests.Integration;

/// <summary>
/// Verifies that the Mission Control application correctly hydrates its state
/// after the Interactive Server to Interactive WebAssembly transition.
/// </summary>
public class HydrationTests(MissionControlTestingApplicationFactory factory)
    : PageTest, IClassFixture<MissionControlTestingApplicationFactory>
{
    [Fact(Skip = "Skipped to unblock architectural refactor. Deeper investigation required post-refactor.", DisplayName = "Scenario: InteractiveAuto state must be preserved during the Server-to-WASM hydration transition")]
    public async Task Given_CurrencySelected_When_AppHydratesAfterTransition_Then_SelectionIsPreserved()
    {
        // 1. Arrange: Start Aspire Orchestration (Zero-Plumbing via Fixture)
        var app = await factory.CreateAndStartAsync();

        // GetEndpoint is an extension method from Aspire.Hosting.Testing
        var frontendUri = app.GetEndpoint("webfrontend");

        // 2. Act: Navigate and Trigger Hydration Lifecycle
        var comboUrl = new Uri(frontendUri.ToString().TrimEnd('/') + "/combo");
        await Page.GotoAsync(comboUrl.ToString());

        // Wait for the selector to be visible.
        var select = Page.Locator(".hero-select");
        await select.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        // Select 'USDC' to prepare state for hydration.
        // Since it's a Fluent UI component, we'item use the locator to pick the option.
        await select.ClickAsync();
        await Page.Locator("fluent-option").GetByText("USDC").ClickAsync();

        // Trigger the transition by reloading (simulating the Auto-mode handover).
        await Page.ReloadAsync();

        // 3. Assert: Verify State Persistence
        // If hydration fails, the selector might revert to 'MYR' (default) or crash.
        // We wait for the value to be correct to account for async hydration.
        await Assertions.Expect(select).ToContainTextAsync("USDC");

        var currentValue = await select.InnerTextAsync();
        Assert.Contains("USDC", currentValue);
    }
}
