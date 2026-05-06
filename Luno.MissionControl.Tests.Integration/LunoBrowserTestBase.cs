using Microsoft.Playwright.Xunit;

namespace Luno.MissionControl.Tests.Integration;

/// <summary>
/// Specialized base class for browser-based integration tests.
/// Provides centralized forensic diagnostic capabilities using Playwright and the LogCollector.
/// </summary>
public abstract class LunoBrowserTestBase : PageTest
{
    /// <summary>
    /// Captures a screenshot and dumps forensic logs to the output directory.
    /// Use this at the end of a test or in a catch block for total observability.
    /// </summary>
    protected async Task CaptureForensicsAsync(MissionControlTestingApplicationFactory factory, string label, bool isFailure = false)
    {
        var status = isFailure ? "failure" : "victory";
        var baseDir = Directory.GetCurrentDirectory();

        // 1. Capture Screenshot
        var screenshotPath = Path.Combine(baseDir, $"forensic-{status}-{label}.png");
        await Page.ScreenshotAsync(new() { Path = screenshotPath, FullPage = true });

        // 2. Capture Filtered Logs (WebFrontend)
        var logs = factory.LogCollector.GetLogs("webfrontend");
        var logPath = Path.Combine(baseDir, $"forensic-{status}-logs-{label}.txt");
        await File.WriteAllLinesAsync(logPath, logs);

        // 3. Capture Unfiltered Stream (Absolute Transparency)
        var allLogs = factory.LogCollector.GetLogs("");
        var allLogPath = Path.Combine(baseDir, $"forensic-{status}-all-unfiltered-{label}.txt");
        await File.WriteAllLinesAsync(allLogPath, allLogs);

        Console.WriteLine($"[FORENSICS] {status.ToUpper()} captured for {label}. Logs: {logPath}");
    }
}
