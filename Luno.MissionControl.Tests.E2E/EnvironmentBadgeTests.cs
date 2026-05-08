using Aspire.Hosting;
using Aspire.Hosting.Testing;
using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace Luno.MissionControl.Tests.E2E;

public class EnvironmentBadgeTests
{
    [Theory(DisplayName = "Scenario: Environment badge correctly reflects host environment status (SSR Smoke Test)")]
    [InlineData("Development", "DEVELOPMENT", "gold-glow")]
    [InlineData("Production", "PRODUCTION", "danger-glow")]
    public async Task Given_EnvironmentSetTo_When_PageLoads_Then_BadgeReflectsStatus(string env, string expectedText, string expectedStatus)
    {
        // --- STAGE 1: INFRASTRUCTURE SETUP ---
        // We use the DistributedApplicationFactory to spin up the AppHost in a controlled environment.
        // This is significantly faster than a full browser E2E test.
        using var factory = new MissionControlTestingApplicationFactory();
        factory.Args = ["--environment", env];
        
        var app = await factory.CreateAndStartAsync();
        var client = app.CreateHttpClient("webfrontend");

        // --- STAGE 2: EXECUTION ---
        // We perform a simple HTTP GET to the root. Since we use InteractiveAuto, 
        // the initial HTML response must contain the SSR-rendered environment badge.
        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var htmlContent = await response.Content.ReadAsStringAsync();

        // --- STAGE 3: SSR PARSING & ASSERTION ---
        // We use AngleSharp to parse the HTML and extract the badge location using CSS selectors.
        // This verifies that the 'First Contentful Paint' is correctly themed.
        var context = BrowsingContext.New(Configuration.Default);
        var parser = context.GetService<IHtmlParser>();
        using var document = await parser.ParseDocumentAsync(htmlContent);

        var bridge = document.QuerySelector(".environment-badge-bridge");
        var statusText = document.QuerySelector(".status-text-target");

        Assert.NotNull(bridge);
        Assert.NotNull(statusText);

        // Verify the data-status attribute for CSS glow effects
        Assert.Equal(expectedStatus, bridge.GetAttribute("data-status"));
        
        // Verify the exact text content (trimmed to ignore SSR whitespace ceremony)
        Assert.Equal(expectedText, statusText.TextContent.Trim());
    }
}
