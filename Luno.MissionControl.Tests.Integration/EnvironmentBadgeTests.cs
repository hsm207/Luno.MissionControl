using Aspire.Hosting;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using Aspire.Hosting.Testing;

namespace Luno.MissionControl.Tests.Integration;

public class EnvironmentBadgeTests(MissionControlTestingApplicationFactory factory) 
    : PageTest, IClassFixture<MissionControlTestingApplicationFactory>
{
    [Theory(DisplayName = "Scenario: Environment badge correctly reflects host environment status and glow effects")]
    [InlineData("Development", "DEVELOPMENT", "rgb(255, 215, 0)")] // Gold Glow
    [InlineData("Production", "PRODUCTION", "rgb(255, 0, 0)")]      // Red Glow
    public async Task Given_EnvironmentSetTo_When_PageLoads_Then_BadgeReflectsStatus(string env, string expectedText, string expectedGlowRgb)
    {
        // 1. Arrange: Start Aspire Orchestration with specific environment
        factory.Args = ["--environment", env];
        var app = await factory.CreateAndStartAsync();
        var frontendUri = app.GetEndpoint("webfrontend");

        // 2. Act: Navigate to the WebFrontend
        await Page.GotoAsync(frontendUri.ToString());

        // 3. Assert: Verify the badge content and style after hydration
        var bridge = Page.Locator(".environment-badge-bridge");
        var statusSpan = bridge.Locator(".status-text-target");
        
        await Expect(statusSpan).ToContainTextAsync(expectedText);
        
        // Verify the CSS Variable Bridge: Check the computed value of --glow-color on the bridge
        var computedGlow = await bridge.EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('--glow-color').trim()");
        
        Assert.Equal(expectedGlowRgb, computedGlow);
    }
}
