using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Luno.MissionControl.Tests.Integration;

public class MissionControlTestingApplicationFactory : DistributedApplicationFactory
{
    public string[]? Args { get; set; }
    public DistributedApplication? App { get; private set; }
    public LogCollector LogCollector { get; } = new();

    public MissionControlTestingApplicationFactory() : base(typeof(Projects.Luno_MissionControl_AppHost)) { }

    protected override void OnBuilderCreating(DistributedApplicationOptions applicationOptions, HostApplicationBuilderSettings hostOptions)
    {
        hostOptions.Configuration ??= new Microsoft.Extensions.Configuration.ConfigurationManager();
        hostOptions.Configuration["Parameters:luno-api-key-id"] = "test";
        hostOptions.Configuration["Parameters:luno-api-key-secret"] = "test";

        hostOptions.Configuration["Logging:LogLevel:Microsoft"] = "Warning";
        hostOptions.Configuration["Logging:LogLevel:System.Net.Http.HttpClient"] = "Warning";
        hostOptions.Configuration["Logging:LogLevel:Luno"] = "Debug";

        if (Args is { Length: > 0 })
        {
            applicationOptions.Args = Args;
        }

        // Disables ANSI color output for the AppHost process to ensure clean log capture during integration tests.
        Environment.SetEnvironmentVariable("Logging__Console__FormatterOptions__ColorBehavior", "Disabled");
        Environment.SetEnvironmentVariable("Logging__Console__DisableColors", "true");
        Environment.SetEnvironmentVariable("NO_COLOR", "true");

        base.OnBuilderCreating(applicationOptions, hostOptions);
    }

    protected override void OnBuilderCreated(DistributedApplicationBuilder applicationBuilder)
    {
        // Injects environment variables into all orchestrated resources to disable console color formatting
        // and prevent ANSI escape sequences from polluting the captured log stream.
        foreach (var resource in applicationBuilder.Resources.OfType<IResourceWithEnvironment>())
        {
            resource.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
            {
                context.EnvironmentVariables["Logging__Console__FormatterOptions__ColorBehavior"] = "Disabled";
                context.EnvironmentVariables["Logging__Console__DisableColors"] = "true";
                context.EnvironmentVariables["NO_COLOR"] = "true";
                context.EnvironmentVariables["DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION"] = "false";
                context.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";
                context.EnvironmentVariables["DOTNET_ENVIRONMENT"] = "Development";
            }));
        }
        base.OnBuilderCreated(applicationBuilder);

        // Register the LogCollector as a logging provider for the AppHost
        applicationBuilder.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddProvider(LogCollector);
        });
    }

    protected override void OnBuilt(DistributedApplication application)
    {
        App = application;
        base.OnBuilt(application);
    }

    public async Task<DistributedApplication> CreateAndStartAsync(CancellationToken ct = default)
    {
        // StartAsync returns Task, not Task<DistributedApplication>!
        await StartAsync(ct);
        
        if (App == null)
        {
            throw new InvalidOperationException("DistributedApplication was not initialized during StartAsync.");
        }
        
        await App.ResourceNotifications.WaitForResourceHealthyAsync("webfrontend")
            .WaitAsync(TimeSpan.FromSeconds(60), ct);
            
        return App;
    }
}
