using Aspire.Hosting;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using Aspire.Hosting.Testing;

namespace Luno.MissionControl.Tests.Integration;

public class EnvironmentBadgeTests() : LunoBrowserTestBase
{
    [Theory(DisplayName = "Scenario: Environment badge correctly reflects host environment status")]
    [InlineData("Development", "DEVELOPMENT", "gold-glow")]
    [InlineData("Production", "PRODUCTION", "danger-glow")]
    public async Task Given_EnvironmentSetTo_When_PageLoads_Then_BadgeReflectsStatus(string env, string expectedText, string expectedStatus)
    {
        StartConsoleLogCapture();

        using var factory = new MissionControlTestingApplicationFactory();
        factory.Args = ["--environment", env];

        try
        {
            var app = await factory.CreateAndStartAsync();
            var frontendUri = app.GetEndpoint("webfrontend");

            await Page.GotoAsync(frontendUri.ToString());

            var bridge = Page.Locator(".environment-badge-bridge");

            // We look for the text anywhere within the bridge container for better resilience
            await Assertions.Expect(bridge).ToContainTextAsync(expectedText, new() { Timeout = 15000 });
            await Assertions.Expect(bridge).ToHaveAttributeAsync("data-status", expectedStatus);

            await CaptureForensicsAsync(factory, $"badge-{env}");
        }
        catch (Exception)
        {
            await CaptureForensicsAsync(factory, $"badge-{env}", isFailure: true);
            throw;
        }
    }
}
