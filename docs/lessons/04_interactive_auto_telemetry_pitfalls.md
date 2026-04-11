# Lesson: Telemetry Hardening for Blazor Interactive-Auto Apps

## 💀 The "Frozen UI" Paradox
In a Blazor Web App using `InteractiveAuto` render mode, the application performs a high-stakes "handover" from the server to the browser's WebAssembly (WASM) runtime.

If the WASM boot sequence fails for *any* reason, the UI remains visible (the server's pre-rendered HTML) but becomes completely non-interactive. This is often caused by misconfigured OpenTelemetry (OTel) pipelines that violate the browser's single-threaded sandbox.

## 🚩 Pitfall 1: The "Trojan Horse" Package
**The Mistake**: Including `OpenTelemetry.Extensions.Hosting` in the `.Client` project to get the `AddOpenTelemetry()` extension method.
**The Consequence**: This package is designed for server environments. It automatically registers background workers (`IHostedService`) and reflection-intensive hooks that the browser runtime prohibits.
**The Symptom**: `ManagedError: AggregateException_ctor_DefaultMessage (Arg_PlatformNotSupported)` during startup.
**The Fix**: Purge `Extensions.Hosting`. Use the base `OpenTelemetry` package or manual registration to avoid illegal background tasks.

## 🚩 Pitfall 2: The "Batch" Default Crash
**The Mistake**: Using the default OTLP exporter configuration, which defaults to `ExportProcessorType.Batch`.
**The Consequence**: Batch exporters use background threads to buffer and flush high volumes of traces. WASM is strictly single-threaded for I/O.
**The Symptom**: The app hangs or throws a `PlatformNotSupported` error the moment an Activity is started.
**The Fix**: **Economy Mode**. Force `ExportProcessorType.Simple` for every signal (Traces, Logs). This ensures telemetry is dispatched on the main thread immediately.

## 🚩 Pitfall 3: The Metrics Zombie 🧟
**The Mistake**: Forgetting about Metrics.
**The Consequence**: Even if you fix Tracing and Logging, the OpenTelemetry SDK often tries to enable default metrics. Metrics aggregation is fundamentally a multi-threaded task.
**The Symptom**: A cryptic `AggregateException` where the console shows traces working but the boot sequence still failing.
**The Fix**: Explicitly disable or empty out the Metrics signal (`.WithMetrics(metrics => { })`) in the Client project.

## 🚩 Pitfall 4: Protocol Ambiguity (CORS & gRPC)
**The Mistake**: Defaulting to the `Grpc` protocol for OTLP exports.
**The Consequence**: Browsers do not support raw gRPC. While gRPC-Web is an option, it requires complex proxying. 
**The Fix**: Use `OtlpExportProtocol.HttpProtobuf`. It is the native choice for browser-to-collector communication.

## 🏆 The "Golden" WASM Configuration Pattern
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("YourSource");
        tracing.AddOtlpExporter(opt =>
        {
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            opt.Endpoint = new Uri("http://localhost:4318");
            opt.ExportProcessorType = ExportProcessorType.Simple;
        });
    })
    .WithMetrics(metrics => { }); // KILL THE ZOMBIE
```

## 🏁 Summary Checklist
- [x] **No** `.Extensions.Hosting` in Client projects.
- [x] **No** `Batch` processors.
- [x] **Protocol** set to `HttpProtobuf`.
- [x] **Metrics** explicitly neutralized.
- [x] **ServiceDefaults** isolated to server-only projects.

> [!IMPORTANT]
> A "quiet" failure in telemetry leads to a "loud" failure in UI interactivity. Always verify the client-side boot with a clean console! 💅
