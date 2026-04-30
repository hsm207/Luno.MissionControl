using Aspire.Hosting.Testing;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Luno.MissionControl.Tests.Integration;

public class EnvironmentBadgeTests : PageTest
{
    [Theory]
    [InlineData("Development", "DEVELOPMENT", "rgb(255, 215, 0)")] // Gold Glow
    [InlineData("Production", "PRODUCTION", "rgb(255, 0, 0)")]      // Red Glow
    public async Task EnvironmentBadge_ReflectsHostEnvironment_InBrowser(string env, string expectedText, string expectedGlowRgb)
    {
        // 1. Arrange: Start Aspire Orchestration with specific environment
        // We use dummy values for required Luno API parameters.
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Luno_MissionControl_AppHost>([
            "--environment", env,
            "Parameters:luno-api-key-id=test", 
            "Parameters:luno-api-key-secret=test"
        ]);
        
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        // 2. Act: Navigate to the WebFrontend
        // We use the 'webfrontend' resource name defined in the AppHost.
        var frontendUri = app.GetEndpoint("webfrontend");
        await Page.GotoAsync(frontendUri.ToString());

        // 3. Assert: Verify the badge content and style after hydration
        var bridge = Page.Locator(".environment-badge-bridge");
        var statusSpan = bridge.Locator(".status-text-target");
        
        // Ensure the text matches the environment
        await Expect(statusSpan).ToContainTextAsync(expectedText);
        
        // Verify the CSS Variable Bridge: Check the computed value of --glow-color on the bridge
        var computedGlow = await bridge.EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('--glow-color').trim()");
        
        Assert.Equal(expectedGlowRgb, computedGlow);
    }
}
