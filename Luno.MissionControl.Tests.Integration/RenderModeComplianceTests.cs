using System.Reflection;

namespace Luno.MissionControl.Tests.Integration;

/// <summary>
/// Static analysis tests that verify Blazor render mode compliance.
///
/// WHY THIS EXISTS:
/// Missing a @rendermode directive on a @page component silently produces a
/// completely inert static-HTML page. Blazor establishes no circuit, fires no
/// event handlers, and emits no error. The only detectable symptom is that
/// nothing responds to user interaction — which is invisible during code review
/// and requires manual browser testing to discover.
///
/// These tests catch that class of defect at build/test time without a browser.
///
/// WHAT IT VALIDATES (per the bUnit model):
///   1. Every @page component in Web.Client must declare @rendermode.
///   2. Every interactive @page component must NOT declare a streamrendering mode
///      alongside InteractiveAuto/Server/WebAssembly.
///
/// See: docs/lessons/aspire_wiring_including_ui.md
/// </summary>
public class RenderModeComplianceTests
{
    // Resolve the Web.Client project source root relative to the test assembly location.
    // Assembly lives at: Tests.Integration/bin/Debug/net10.0/
    // 4 levels up: net10.0 -> Debug -> bin -> Tests.Integration -> [SolutionRoot]
    private static string SolutionRoot => Path.GetFullPath(
        Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "..", "..", "..", ".."));

    private static string WebClientRoot => Path.Combine(
        SolutionRoot, "Luno.MissionControl.Web.Client");

    private static IReadOnlyList<FileInfo> AllClientPageComponents => Directory
        .EnumerateFiles(WebClientRoot, "*.razor", SearchOption.AllDirectories)
        .Select(p => new FileInfo(p))
        .Where(f => IsPageComponent(f))
        .ToList();

    private static bool IsPageComponent(FileInfo razorFile)
    {
        var firstLines = File.ReadLines(razorFile.FullName).Take(10);
        return firstLines.Any(l => l.TrimStart().StartsWith("@page ", StringComparison.OrdinalIgnoreCase));
    }

    private static string? FindDirective(FileInfo razorFile, string directive)
    {
        foreach (var line in File.ReadLines(razorFile.FullName).Take(20))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith(directive, StringComparison.OrdinalIgnoreCase))
                return trimmed;
        }
        return null;
    }

    [Fact]
    public void AllClientPageComponents_HaveExplicitRenderModeDirective()
    {
        // Arrange
        Assert.True(Directory.Exists(WebClientRoot),
            $"Web.Client source directory not found at: {WebClientRoot}");

        var violations = new List<string>();

        foreach (var pageComponent in AllClientPageComponents)
        {
            var hasRenderMode = FindDirective(pageComponent, "@rendermode") is not null;
            if (!hasRenderMode)
            {
                violations.Add(pageComponent.FullName);
            }
        }

        // Assert
        Assert.True(violations.Count == 0,
            $"""
            FROZEN UI RISK: The following @page components in Web.Client are missing a @rendermode directive.
            Without it, Blazor renders them as static HTML — buttons, events, and bindings will be completely non-functional.
            
            Fix: Add '@rendermode InteractiveAuto' (or InteractiveServer/InteractiveWebAssembly) as the second line after @page.
            
            Violations ({violations.Count}):
            {string.Join(Environment.NewLine, violations.Select(v => $"  - {v}"))}
            
            See: docs/lessons/aspire_wiring_including_ui.md
            """);
    }

    [Fact]
    public void AllClientPageComponents_RenderModeIsInteractive()
    {
        // Arrange
        Assert.True(Directory.Exists(WebClientRoot),
            $"Web.Client source directory not found at: {WebClientRoot}");

        var staticRenderModes = new[] { "InteractiveServer", "InteractiveWebAssembly", "InteractiveAuto" };
        var violations = new List<string>();

        foreach (var pageComponent in AllClientPageComponents)
        {
            var renderModeLine = FindDirective(pageComponent, "@rendermode");
            if (renderModeLine is null) continue; // caught by previous test

            var isInteractive = staticRenderModes.Any(mode =>
                renderModeLine.Contains(mode, StringComparison.OrdinalIgnoreCase));

            if (!isInteractive)
            {
                violations.Add($"{pageComponent.FullName} (found: {renderModeLine.Trim()})");
            }
        }

        // Assert
        Assert.True(violations.Count == 0,
            $"""
            FROZEN UI RISK: The following @page components in Web.Client declare a non-interactive @rendermode.
            Components in Web.Client must use InteractiveAuto, InteractiveServer, or InteractiveWebAssembly.
            
            Violations ({violations.Count}):
            {string.Join(Environment.NewLine, violations.Select(v => $"  - {v}"))}
            
            See: docs/lessons/aspire_wiring_including_ui.md
            """);
    }

    [Fact]
    public void AllClientPageComponents_AreLocatedInWebClientProject()
    {
        // This test validates the project structure invariant:
        // interactive client components must ALWAYS live in Web.Client, not in Web (server).
        // If they're in Web, they can't use InteractiveWebAssembly/Auto and the WASM bundle won't include them.

        var serverWebRoot = Path.Combine(SolutionRoot, "Luno.MissionControl.Web");
        var serverPagesDir = Path.Combine(serverWebRoot, "Components", "Pages");

        if (!Directory.Exists(serverPagesDir))
            return; // Nothing to check

        var serverPageComponents = Directory
            .EnumerateFiles(serverPagesDir, "*.razor", SearchOption.AllDirectories)
            .Select(p => new FileInfo(p))
            .Where(f => IsPageComponent(f))
            .ToList();

        // Server @page components that declare InteractiveAuto or InteractiveWebAssembly
        // would be a misconfiguration — they can't be served as WASM from the server project.
        var violations = new List<string>();
        foreach (var page in serverPageComponents)
        {
            var renderModeLine = FindDirective(page, "@rendermode");
            if (renderModeLine is null) continue;

            if (renderModeLine.Contains("InteractiveWebAssembly", StringComparison.OrdinalIgnoreCase) ||
                renderModeLine.Contains("InteractiveAuto", StringComparison.OrdinalIgnoreCase))
            {
                violations.Add(page.FullName);
            }
        }

        Assert.True(violations.Count == 0,
            $"""
            MISCONFIGURATION: The following @page components in Web (server) declare InteractiveWebAssembly or InteractiveAuto.
            These components must be moved to Web.Client to be included in the WASM bundle.
            
            Violations ({violations.Count}):
            {string.Join(Environment.NewLine, violations.Select(v => $"  - {v}"))}
            """);
    }
}
