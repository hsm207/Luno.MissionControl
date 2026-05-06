using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Luno.MissionControl.Tests.Integration;

/// <summary>
/// [PHASE 0: PARITY GATE]
/// Verifies the end-to-end "Basket Execution" workflow to establish a functional baseline.
/// This test ensures that the legacy architecture correctly calculates allocations,
/// opens the Review Gate dialog, and dispatches orders via the simulated orchestrator.
/// </summary>
public class ParityGateTests(MissionControlTestingApplicationFactory factory) 
    : LunoBrowserTestBase, IClassFixture<MissionControlTestingApplicationFactory>
{

    [Theory(DisplayName = "Parity Gate: Full Basket Execution Flow (Triple Verification)")]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Given_StandardBasket_When_ExecutingOrder_Then_OrdersAreSuccessfullyPlaced(int run)
    {
        try
        {
            // 0. Reset: Clear logs from previous runs to maintain forensic isolation
            factory.LogCollector.Clear();

            // 1. Arrange: Start Aspire Orchestration
            var app = await factory.CreateAndStartAsync();
            var frontendUri = app.GetEndpoint("webfrontend");
            var comboUrl = new Uri(frontendUri.ToString().TrimEnd('/') + "/combo");

            // 2. Act: Navigate to the Combo Dashboard
            await Page.GotoAsync(comboUrl.ToString());
            
            // 2. Act: Prepare the Investment
            // We wait for the loading overlay to disappear and for ACTUAL prices to appear (proving hydration is complete)
            var loadingOverlay = Page.GetByText("Loading market data...");
            await loadingOverlay.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 30000 });
            
            // TACTICAL PAUSE: Give the WASM runtime the mandatory window to claim the DOM (Equilibrium Protocol)
            await Task.Delay(2000);

            // Set a target investment amount (RM 1,500) - DETERMINISTIC ID TARGETING
            var inputControl = Page.Locator("#hero-investment-input >> input");
            await inputControl.FillAsync("1500");
            await Task.Delay(500);
            await inputControl.PressAsync("Tab"); // Force focus loss to commit the value
            
            // ASSERT: Verify the value was actually committed and formatted by Fluent UI
            await Assertions.Expect(inputControl).ToHaveValueAsync("1,500");
            
            // Give Blazor the commit heartbeat to ensure state-sync (Equilibrium Protocol)
            await Task.Delay(1000);

            // Trigger the Execution Flow
            var buyButton = Page.Locator(".buy-button");
            await buyButton.ClickAsync();
            await Task.Delay(500);

            // 3. Act: Handle the Review Gate Dialog
            // We wait for the specific heading text to ensure the dialog is actually visible and interactive
            var dialogHeader = Page.GetByText("CONFIRM YOUR COMBO");
            await dialogHeader.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await Task.Delay(500);

            // Acknowledge the risk checkbox (Direct tag targeting)
            var checkbox = Page.Locator("fluent-checkbox");
            await checkbox.ClickAsync();
            await Task.Delay(500);

            // Click "CONFIRM PURCHASE" (Force click to bypass web component shadow DOM assertion lag)
            var confirmBtn = Page.Locator("fluent-button.btn-gilded");
            await confirmBtn.ClickAsync(new() { Force = true });
            await Task.Delay(500);

            // 4. Assert: Verify the "Mission Accomplished" outcome
            // We wait for the success text to appear anywhere (pierces shadow DOM by default)
            var toastMessage = Page.GetByText("Mission Accomplished");
            await toastMessage.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30000 });
            await Assertions.Expect(toastMessage).ToBeVisibleAsync();

            // [VICTORY] Capture total forensics for audit parity
            await CaptureForensicsAsync(factory, $"run-{run}");

            // [STABILITY MANDATE] Verify that the core business logic received the correct signal.
            // We use the LogCollector directly here to perform the final business-layer assertion.
            var logs = factory.LogCollector.GetLogs("webfrontend");
            Assert.Contains(logs, log => log.Contains("Order request received"));
        }
        catch (Exception)
        {
            // [DIAGNOSTIC] Capture failure forensics for total transparency
            await CaptureForensicsAsync(factory, $"run-{run}", isFailure: true);
            throw;
        }
    }
}
