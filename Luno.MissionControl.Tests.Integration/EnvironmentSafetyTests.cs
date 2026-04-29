using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Luno.MissionControl.Tests.Integration;

public class EnvironmentSafetyTests
{
    [Theory(DisplayName = "Verify that webfrontend service identifies the correct environment ('Development' vs 'Production') in startup logs")]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task GivenEnvironment_WhenStarting_LogsCorrectEnvironment(string environment)
    {
        // Arrange
        // We pass the environment argument to the AppHost via the builder.
        // We also provide dummy values for required Luno API parameters to ensure orchestration starts successfully.
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Luno_MissionControl_AppHost>(
            [ 
                "--environment", environment,
                "Parameters:luno-api-key-id=dummy-id",
                "Parameters:luno-api-key-secret=dummy-secret"
            ]
        );

        // We wire a custom LogCollector to intercept logs from the resources.
        // This allows us to verify service behavior without UI or network dependencies. #CleanCode
        var logCollector = new LogCollector();
        builder.Services.AddLogging(logging => 
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddProvider(logCollector);
        });

        await using var app = await builder.BuildAsync();
        
        // Act
        await app.StartAsync();

        // Wait for the webfrontend resource to reach a healthy state.
        // This ensures the application has started and emitted its initial hosting logs.
        await app.ResourceNotifications.WaitForResourceHealthyAsync("webfrontend").WaitAsync(TimeSpan.FromSeconds(30));

        // Assert
        // We check the captured logs for the "Hosting environment" message.
        var logs = logCollector.GetLogs("webfrontend");
        
        Assert.NotEmpty(logs);
        Assert.Contains(logs, log => log.Contains($"Hosting environment: {environment}", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// A thread-safe log collector that stores logs indexed by their category name.
    /// </summary>
    private class LogCollector : ILoggerProvider
    {
        private readonly ConcurrentBag<(string Category, string Message)> _allLogs = new();

        public ILogger CreateLogger(string categoryName) => new ResourceLogger(categoryName, _allLogs);

        public void Dispose() { }

        public IEnumerable<string> GetLogs(string resourceName)
        {
            // Aspire resource logs are typically categorized under the resource path in the AppHost.
            return _allLogs
                .Where(l => l.Category.Contains(resourceName, StringComparison.OrdinalIgnoreCase))
                .Select(l => l.Message);
        }

        private class ResourceLogger(string categoryName, ConcurrentBag<(string Category, string Message)> allLogs) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var message = formatter(state, exception);
                allLogs.Add((categoryName, message));
            }
        }
    }
}
