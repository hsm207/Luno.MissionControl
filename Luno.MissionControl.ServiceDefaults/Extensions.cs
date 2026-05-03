using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Standard Aspire service defaults for .NET 10.
/// These extension methods configure OpenTelemetry, Resilience, and Service Discovery.
/// </summary>
public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Add the OTLP forwarder for WASM telemetry
        builder.AddOtlpForwarder();

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("Microsoft.AspNetCore.Components")
                    .AddMeter("Microsoft.AspNetCore.Components.Lifecycle")
                    .AddMeter("Microsoft.AspNetCore.Components.Server.Circuits")
                    .AddMeter("Luno.MissionControl.*");
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(Luno.MissionControl.Application.ForensicTracing.SourceName)
                    .AddSource("Microsoft.AspNetCore.Components")
                    .AddSource("Microsoft.AspNetCore.Components.Server.Circuits")
                    .AddSource("Microsoft.AspNetCore.SignalR.Server")
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // Filter out noise from health checks and SignalR hub heartbeats
                        options.Filter = (httpContext) =>
                        {
                            var path = httpContext.Request.Path.Value ?? string.Empty;
                            return path != "/health" && path != "/alive" && !path.StartsWith("/pricehub");
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        // Filter out outgoing telemetry traffic to the dashboard to avoid recursive tracing
                        options.FilterHttpRequestMessage = (req) =>
                        {
                            return !req.RequestUri?.PathAndQuery.Contains("/_otlp") ?? true;
                        };

                        // Enrich spans with Method + Path for better observability in the dashboard
                        options.EnrichWithHttpRequestMessage = (activity, request) =>
                        {
                            activity.DisplayName = $"{request.Method} {request.RequestUri?.AbsolutePath}";
                        };
                    });
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Registers the OTLP forwarder service and its dependencies.
    /// </summary>
    public static IHostApplicationBuilder AddOtlpForwarder(this IHostApplicationBuilder builder)
    {
        var endpoint = builder.Configuration["ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"];
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            var rawHeaders = builder.Configuration["OTEL_EXPORTER_OTLP_HEADERS"];

            builder.Services.AddHttpClient("OtlpForwarder");
            builder.Services.AddSingleton(sp => new OtlpForwarder(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("OtlpForwarder"),
                sp.GetRequiredService<ILogger<OtlpForwarder>>(),
                endpoint,
                rawHeaders));
        }
        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks routes to matching the Aspire dashboard expected endpoints
        app.MapHealthChecks("/health").DisableHttpMetrics();
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        }).DisableHttpMetrics();

        return app;
    }

    /// <summary>
    /// Maps a transparent proxy for OTLP HTTP traffic from client-side (WASM) applications.
    /// Uses the OtlpForwarder service to handle the actual proxying logic.
    /// </summary>
    public static WebApplication MapOtlpForwarder(this WebApplication app)
    {
        if (app.Services.GetService<OtlpForwarder>() is not null)
        {
            app.MapPost("/_otlp/{**path}", async (HttpContext context, string path, OtlpForwarder forwarder) =>
                await forwarder.ForwardAsync(path, context));
        }

        return app;
    }
}
