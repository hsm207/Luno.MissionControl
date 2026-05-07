using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http;
using Microsoft.FluentUI.AspNetCore.Components;
using Luno.MissionControl.Application;
using Luno.MissionControl.Application.Ports;
using Luno.MissionControl.Application.Diagnostics;
using Luno.MissionControl.Web.Client.Adapters;
using Luno.MissionControl.Web.Client.Infrastructure;
using Luno.MissionControl.Web.Client.Services;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

builder.Services.AddFluentUIComponents(config =>
{
    config.MarkupSanitized.SanitizeInlineStyle = (value) => value;
});

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
    Timeout = TimeSpan.FromMinutes(2)
});
builder.Services.AddHostEnvironmentBridge(builder.HostEnvironment.Environment, "Luno.MissionControl.Web.Client");

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

// We use the Simple Export Processor for OpenTelemetry in WASM to minimize memory 
// and CPU overhead in the single-threaded browser sandbox environment.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Luno.MissionControl.Web.Client"))
    .WithTracing(tracing =>
    {
        tracing.AddSource(ForensicTracing.SourceName);
        tracing.AddSource("Microsoft.AspNetCore.SignalR.Client");
        tracing.AddOtlpExporter(opt =>
        {
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            opt.Endpoint = new Uri(builder.HostEnvironment.BaseAddress + "_otlp/v1/traces");
            opt.ExportProcessorType = ExportProcessorType.Simple;
        });
    })
    .WithLogging(logging =>
    {
        logging.AddOtlpExporter(opt =>
        {
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            opt.Endpoint = new Uri(builder.HostEnvironment.BaseAddress + "_otlp/v1/logs");
            opt.ExportProcessorType = ExportProcessorType.Simple;
        });
    })
    .WithMetrics(metrics => { }); // Metrics MUST be disabled/empty to prevent background threads.

builder.Services.AddPriceHubClient();

builder.Services.AddScoped<ClientBasketState>();
builder.Services.AddScoped<IBasketState>(sp => sp.GetRequiredService<ClientBasketState>());
builder.Services.AddScoped<IPriceClient>(sp => sp.GetRequiredService<ClientBasketState>());

builder.Services.AddScoped<IBasketService, BasketServiceProxy>();

builder.Services.AddScoped<Luno.MissionControl.Web.Client.Components.Layout.MainLayoutViewModel>();


var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Luno Mission Control WASM initialized. Environment: {Env}", builder.HostEnvironment.Environment);

await app.RunAsync();
