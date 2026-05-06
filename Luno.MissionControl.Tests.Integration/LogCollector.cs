using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Luno.MissionControl.Tests.Integration;

/// <summary>
/// A thread-safe log collector that stores logs indexed by their category name.
/// This enables deterministic verification of business signals emitted by orchestrated resources.
/// </summary>
public sealed class LogCollector : ILoggerProvider
{
    private readonly ConcurrentQueue<(DateTime Timestamp, string Category, string Message)> _allLogs = [];

    public ILogger CreateLogger(string categoryName) => new ResourceLogger(categoryName, _allLogs);

    public void Dispose() { }

    /// <summary>
    /// Retrieves all logs captured for a specific resource.
    /// </summary>
    /// <param name="resourceName">The name of the Aspire resource (e.g., "webfrontend").</param>
    /// <returns>A collection of log messages.</returns>
    public IEnumerable<string> GetLogs(string resourceName)
    {
        return _allLogs
            .Where(l => string.IsNullOrEmpty(resourceName) || l.Category.Contains(resourceName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(l => l.Timestamp)
            .Select(l => $"[{l.Timestamp:HH:mm:ss.fff}] {l.Category}: {l.Message}");
    }

    /// <summary>
    /// Clears all captured logs.
    /// </summary>
    public void Clear() => _allLogs.Clear();

    private sealed class ResourceLogger(string categoryName, ConcurrentQueue<(DateTime Timestamp, string Category, string Message)> allLogs) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            allLogs.Enqueue((DateTime.UtcNow, categoryName, message));
        }
    }
}
