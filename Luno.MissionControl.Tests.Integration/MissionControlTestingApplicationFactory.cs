using Aspire.Hosting;
using Aspire.Hosting.Testing;
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

            // Explicitly sync the environment name from Args to ensure the AppHost builder reflects the intended test environment.
            for (int i = 0; i < Args.Length - 1; i++)
            {
                if (Args[i] == "--environment" || Args[i] == "-e")
                {
                    hostOptions.EnvironmentName = Args[i + 1];
                    break;
                }
            }
        }

        // Disable ANSI color output to ensure clean log capture for forensic analysis.
        Environment.SetEnvironmentVariable("Logging__Console__FormatterOptions__ColorBehavior", "Disabled");
        Environment.SetEnvironmentVariable("Logging__Console__DisableColors", "true");
        Environment.SetEnvironmentVariable("NO_COLOR", "true");
        Environment.SetEnvironmentVariable("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION", "false");

        base.OnBuilderCreating(applicationOptions, hostOptions);
    }

    protected override void OnBuilderCreated(DistributedApplicationBuilder applicationBuilder)
    {
        base.OnBuilderCreated(applicationBuilder);

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
