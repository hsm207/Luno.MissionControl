using System.Diagnostics;

namespace Luno.MissionControl.Application.Diagnostics;

/// <summary>
/// Provides a unified, high-stakes forensic tracing source for the entire Mission Control ecosystem.
/// This is located in the Application project to ensure compatibility with both Server and WASM Client runtimes.
/// </summary>
public static class ForensicTracing
{
    public const string SourceName = "Luno.MissionControl.Forensics";

    // The activity source for the application.
    public static readonly ActivitySource ActivitySource = new(SourceName);

    /// <summary>
    /// Starts a new forensic activity span.
    /// </summary>
    public static Activity? StartActivity(string name) => ActivitySource.StartActivity(name);
}
