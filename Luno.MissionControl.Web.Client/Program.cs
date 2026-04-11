using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Net.Http;
using Microsoft.FluentUI.AspNetCore.Components;
using Luno.MissionControl.Application;
using Luno.MissionControl.Web.Client.Services;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using OpenTelemetry.Exporter;
using Microsoft.Extensions.Logging;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddFluentUIComponents();

// 0. Base Connectivity
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// 1. Economy Telemetry Mandate (WASM Browser Sandbox Configuration)
// We isolate signals and use explicit exporters to ensure the 'Simple' processor is 
// used everywhere. We avoid cross-cutting 'UseOtlpExporter' to prevent 'Batch' defaults.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource(ForensicTracing.SourceName);
        tracing.AddOtlpExporter(opt =>
        {
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            opt.Endpoint = new Uri(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4318");
            opt.ExportProcessorType = ExportProcessorType.Simple;
        });
    })
    .WithMetrics(metrics => { }); // Metrics MUST be disabled/empty to prevent background threads.
// Logging is handled by the default provider to minimize WASM boot complexity.

// 3. Core State & Connectivity
builder.Services.AddScoped<ClientBasketState>();
builder.Services.AddScoped<IBasketState>(sp => sp.GetRequiredService<ClientBasketState>());
builder.Services.AddScoped<IPriceClient>(sp => sp.GetRequiredService<ClientBasketState>());

// 4. Hub-agnostic Orchestration Proxy
builder.Services.AddScoped<IBasketService, BasketServiceProxy>();

await builder.Build().RunAsync();
