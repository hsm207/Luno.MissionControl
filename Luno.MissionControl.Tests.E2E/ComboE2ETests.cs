using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Luno.MissionControl.Tests.E2E;

/// <summary>
/// Verifies the end-to-end "Basket Execution" workflow to establish a functional baseline.
/// </summary>
public class ComboE2ETests(MissionControlTestingApplicationFactory factory)
    : LunoBrowserTestBase, IClassFixture<MissionControlTestingApplicationFactory>
{

    /// <remarks>
    /// NOTE: This test is sensitive to resource contention when executed in parallel with other AppHost-based tests.
    /// To ensure 100% certainty that the logic is not broken, run this test in isolation 
    /// (e.g., using 'dotnet test --filter "FullyQualifiedName~ComboE2ETests"').
    /// </remarks>
    [Theory(DisplayName = "Scenario: Full lifecycle of a Combo Basket Order from asset selection to execution")]
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

            // --- STAGE 1: BOOTSTRAP & INITIAL STATE ---
            // We start by navigating to the 'Combo' page and verifying the MYR defaults.
            // This ensures our 'Warm Cache' bootstrap in MarketInventory is providing immediate metadata.
            await Page.GotoAsync(comboUrl.ToString());
            await Task.Delay(2000); // Allow SignalR/UI circuit to hydrate

            var currencySelect = Page.Locator(".hero-select");
            var assetRows = Page.Locator(".asset-name");

            await Assertions.Expect(currencySelect).ToContainTextAsync("MYR");
            await Assertions.Expect(assetRows).ToHaveCountAsync(2);
            await Assertions.Expect(assetRows.Nth(0)).ToContainTextAsync("XBT / MYR");

            // --- STAGE 2: ASSET EXPANSION ---
            // We search for and add 'XRP' to the basket. This verifies the 'Find Crypto' search 
            // logic and the dynamic expansion of the allocation grid.
            var searchBox = Page.Locator(".search-box input");
            await searchBox.FocusAsync();
            await searchBox.PressSequentiallyAsync("XRP", new() { Delay = 50 });
            await Task.Delay(2000); // Wait for API debounce and results
            await Page.Keyboard.PressAsync("ArrowDown");
            await Page.Keyboard.PressAsync("Enter");
            await Page.Locator(".add-coin-button").ClickAsync();
            await Task.Delay(1000);

            await Assertions.Expect(assetRows).ToHaveCountAsync(3);
            await Assertions.Expect(assetRows.Nth(2)).ToContainTextAsync("XRP / MYR");

            // --- STAGE 3: CURRENCY TRANSITION & REHYDRATION ---
            // Now for the 'Survival of the Fittest' challenge! We transition from MYR to USDC.
            // Since XRPUSDC doesn't exist, the orchestrator must drop it and rehydrate only XBT and ETH.
            await currencySelect.ClickAsync();
            await Page.Locator("fluent-option").GetByText("USDC").ClickAsync();
            await Task.Delay(2000); // Wait for transition and inventory lookup

            await Assertions.Expect(currencySelect).ToContainTextAsync("USDC");
            await Assertions.Expect(assetRows).ToHaveCountAsync(2);
            await Assertions.Expect(assetRows.Nth(0)).ToContainTextAsync("XBT / USDC");
            await Assertions.Expect(assetRows.Nth(1)).ToContainTextAsync("ETH / USDC");

            // --- STAGE 4: PRECISION WEIGHTING ---
            // We set the investment amount and push the system to its mathematical limits 
            // with a high-precision 99.99% / 0.01% allocation.
            var loadingOverlay = Page.GetByText("Loading market data...");
            await loadingOverlay.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 60000 });

            var inputControl = Page.Locator("#hero-investment-input >> input");
            await inputControl.FocusAsync();
            await Page.Keyboard.PressAsync("Control+A");
            await Page.Keyboard.PressAsync("Backspace");
            await inputControl.PressSequentiallyAsync("1500", new() { Delay = 50 });
            await inputControl.PressAsync("Tab");

            var xbtInput = Page.Locator("#weight-input-XBTUSDC >> input");
            await xbtInput.FocusAsync();
            await Page.Keyboard.PressAsync("Control+A");
            await Page.Keyboard.PressAsync("Backspace");
            await xbtInput.PressSequentiallyAsync("99.99", new() { Delay = 50 });
            await xbtInput.PressAsync("Tab");

            var ethInput = Page.Locator("#weight-input-ETHUSDC >> input");
            await ethInput.FocusAsync();
            await Page.Keyboard.PressAsync("Control+A");
            await Page.Keyboard.PressAsync("Backspace");
            await ethInput.PressSequentiallyAsync("0.01", new() { Delay = 50 });
            await ethInput.PressAsync("Tab");
            await Task.Delay(1000);

            // Verify that the values are actually stored and reflected in the inputs
            await Assertions.Expect(inputControl).ToHaveValueAsync("1500");
            await Assertions.Expect(xbtInput).ToHaveValueAsync("99.99");
            await Assertions.Expect(ethInput).ToHaveValueAsync("0.01");

            // --- STAGE 5: FINAL EXECUTION ---
            // We initiate the order placement, confirm the review gate, and verify the 'Mission Accomplished' toast.
            var buyButton = Page.Locator(".buy-button");
            await Assertions.Expect(buyButton).ToBeEnabledAsync();
            await buyButton.ClickAsync();
            await Task.Delay(500);

            var dialogHeader = Page.GetByText("CONFIRM YOUR COMBO");
            await dialogHeader.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30000 });

            await Page.Locator("fluent-checkbox").ClickAsync();
            await Task.Delay(500);

            // Force click to bypass web component shadow DOM assertion lag in CI
            await Page.Locator("fluent-button.btn-gilded").ClickAsync(new() { Force = true });
            await Task.Delay(500);

            var toastMessage = Page.GetByText("Mission Accomplished");
            await toastMessage.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60000 });
            await Assertions.Expect(toastMessage).ToBeVisibleAsync();

            // --- STAGE 6: FORENSIC CAPTURE ---
            // Finally, we capture the browser state and verify the backend log signals.
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
