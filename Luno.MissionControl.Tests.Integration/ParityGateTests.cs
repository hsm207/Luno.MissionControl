using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Luno.MissionControl.Tests.Integration;

/// <summary>
/// Verifies the end-to-end "Basket Execution" workflow to establish a functional baseline.
/// </summary>
public class ParityGateTests(MissionControlTestingApplicationFactory factory) 
    : LunoBrowserTestBase, IClassFixture<MissionControlTestingApplicationFactory>
{

    /// <remarks>
    /// NOTE: This test is sensitive to resource contention when executed in parallel with other AppHost-based tests.
    /// To ensure 100% certainty that the integration logic is not broken, run this test in isolation 
    /// (e.g., using 'dotnet test --filter "FullyQualifiedName~ParityGateTests"').
    /// </remarks>
    [Theory(DisplayName = "Parity Gate: Full Basket Execution Flow (Triple Verification)")]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Given_StandardBasket_When_ExecutingOrder_Then_OrdersAreSuccessfullyPlaced(int run)
    {
        try
        {
            factory.LogCollector.Clear();

            var app = await factory.CreateAndStartAsync();
            var frontendUri = app.GetEndpoint("webfrontend");
            var comboUrl = new Uri(frontendUri.ToString().TrimEnd('/') + "/combo");

            await Page.GotoAsync(comboUrl.ToString());
            
            var loadingOverlay = Page.GetByText("Loading market data...");
            await loadingOverlay.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 60000 });
            await Task.Delay(2000);

            var inputControl = Page.Locator("#hero-investment-input >> input");
            await inputControl.ClickAsync();
            await inputControl.FillAsync("1500");
            await Task.Delay(500);
            await inputControl.PressAsync("Tab");
            
            await Assertions.Expect(inputControl).ToHaveValueAsync(new System.Text.RegularExpressions.Regex("1,?500"));
            await Task.Delay(1000);

            var buyButton = Page.Locator(".buy-button");
            await buyButton.ClickAsync();
            await Task.Delay(500);

            var dialogHeader = Page.GetByText("CONFIRM YOUR COMBO");
            await dialogHeader.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30000 });
            await Task.Delay(500);

            var checkbox = Page.Locator("fluent-checkbox");
            await checkbox.ClickAsync();
            await Task.Delay(500);

            // Force click to bypass web component shadow DOM assertion lag
            var confirmBtn = Page.Locator("fluent-button.btn-gilded");
            await confirmBtn.ClickAsync(new() { Force = true });
            await Task.Delay(500);

            var toastMessage = Page.GetByText("Mission Accomplished");
            await toastMessage.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60000 });
            await Assertions.Expect(toastMessage).ToBeVisibleAsync();

            await CaptureForensicsAsync(factory, $"run-{run}");

            var logs = factory.LogCollector.GetLogs("webfrontend");
            Assert.Contains(logs, log => log.Contains("Order request received"));
        }
        catch (Exception)
        {
            await CaptureForensicsAsync(factory, $"run-{run}", isFailure: true);
            throw;
        }
    }
}
