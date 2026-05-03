using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.Hosting;

namespace Luno.MissionControl.Tests.Integration;

public class MissionControlTestingApplicationFactory : DistributedApplicationFactory
{
    public string[]? Args { get; set; }
    public DistributedApplication? App { get; private set; }

    public MissionControlTestingApplicationFactory() : base(typeof(Projects.Luno_MissionControl_AppHost)) { }

    protected override void OnBuilderCreating(DistributedApplicationOptions applicationOptions, HostApplicationBuilderSettings hostOptions)
    {
        hostOptions.Configuration ??= new Microsoft.Extensions.Configuration.ConfigurationManager();
        hostOptions.Configuration["Parameters:luno-api-key-id"] = "test";
        hostOptions.Configuration["Parameters:luno-api-key-secret"] = "test";

        hostOptions.Configuration["Logging:LogLevel:Microsoft"] = "Warning";
        hostOptions.Configuration["Logging:LogLevel:System.Net.Http.HttpClient"] = "Warning";

        if (Args is { Length: > 0 })
        {
            applicationOptions.Args = Args;
        }

        base.OnBuilderCreating(applicationOptions, hostOptions);
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
