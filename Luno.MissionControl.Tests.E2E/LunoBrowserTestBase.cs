using Microsoft.Playwright.Xunit;

namespace Luno.MissionControl.Tests.E2E;

/// <summary>
/// Specialized base class for browser-based integration tests.
/// Provides centralized forensic diagnostic capabilities using Playwright and the LogCollector.
/// </summary>
public abstract class LunoBrowserTestBase : PageTest
{
    private readonly List<string> _consoleLogs = [];

    protected void StartConsoleLogCapture()
    {
        Page.Console += (_, e) => _consoleLogs.Add($"[{e.Type}] {e.Text}");
    }

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

        // 2. Capture Browser Console Logs
        var browserLogPath = Path.Combine(baseDir, $"forensic-{status}-browser-{label}.txt");
        await File.WriteAllLinesAsync(browserLogPath, _consoleLogs);

        // 3. Capture Filtered Logs (WebFrontend)
        var logs = factory.LogCollector.GetLogs("webfrontend");
        var logPath = Path.Combine(baseDir, $"forensic-{status}-logs-{label}.txt");
        await File.WriteAllLinesAsync(logPath, logs);

        // 4. Capture Unfiltered Stream (Absolute Transparency)
        var allLogs = factory.LogCollector.GetLogs("");
        var allLogPath = Path.Combine(baseDir, $"forensic-{status}-all-unfiltered-{label}.txt");
        await File.WriteAllLinesAsync(allLogPath, allLogs);

        Console.WriteLine($"[FORENSICS] {status.ToUpper()} captured for {label}. Browser logs: {browserLogPath}");
    }
}
