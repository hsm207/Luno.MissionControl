# Lesson: UI Stability and WASM Telemetry Hardening

## The Incident (The "Ghost in the Machine")

During the stabilization of the `Luno.MissionControl` dashboard, the `BasketArchitect` component appeared functional but remained fundamentally broken in a subtle, maddening way. Users attempting to add an asset (e.g., `SOLZAR`) would find the dropdown "snapping back" to the first index (`XBTZAR`) unexpectedly.

Compound failures in the telemetry pipeline further masked the issue:
1. **The WASM Crash (`Arg_PlatformNotSupported`)**: The OpenTelemetry configuration initially used the default `Batch` processor, which attempts to spawn background threads—a violation of the WASM browser sandbox.
2. **The Configuration Paradox**: Redundant OTLP exporter registrations caused an `AggregateException` at boot, preventing the application from initializing.

## Root Cause Analysis

### Failure 1: The "Binding Paradox" (Ghost Selection)
The `FluentSelect` component was bound to a local state variable (`_selectedPair`). High-frequency market data updates (via SignalR) triggered `StateHasChanged()` on the parent component. Because there was no explicit **Placeholder Option** in the select list, the browser’s DOM-diffing logic reset the dropdown to its first valid index (`XBTZAR`) during the re-render.

**The Fix**: 
- **Subtree Isolation**: Move high-frequency updates into a dedicated `PriceLabel` component to prevent global re-renders from affecting user input state.
- **Explicit Placeholders**: Add a `<FluentOption Value="">Select an asset...</FluentOption>` to ensure the "unselected" state is distinct and stable.

### Failure 2: WASM Telemetry "Simple" Mandate
OpenTelemetry's `BatchExportProcessor` is incompatible with the single-threaded browser environment. It fails silently or throws `PlatformNotSupported` because it cannot manage the required background worker threads.

**The Fix**: You MUST use `ExportProcessorType.Simple` and `OtlpExportProtocol.HttpProtobuf` for WASM telemetry.

## Why We Stumbled (The Honest Retro)

1. **Assumption of Linear Stability**: We assumed that because the SignalR *connection* worked, the *state* would be stable. We ignored the "State Jitter" caused by background data patches.
2. **Sandbox Blindness**: We attempted to port Server-side OTel patterns (`Batch` exporters) directly into the client project without accounting for the browser's execution limits.
3. **Guessing vs. Auditing**: We initially hypothesized about CSS focus or Z-index bugs instead of utilizing the available **Forensic Tracing** tools to observe the actual data flow.

## What Got Us Unstuck? (The "Smoking Gun")

The breakthrough happened the moment we added **Forensic Activity Spans** to the UI events:
```csharp
using var activity = ForensicTracing.StartActivity("Asset Selected in Dropdown");
activity?.SetTag("pair.id", _selectedPair);
```
**The Discovery**: The traces showed that `_selectedPair` was being reset to `XBTZAR` *automatically* right after a price tick arrived. The data proved it wasn't a "click failure"—it was a "state overwrite" caused by the Blazor rendering lifecycle.

## Actionable Guardrails

1. **Isolate High-Frequency Renders**: If a value updates frequently, it MUST live in its own component (Humble View). Never trigger a full `StateHasChanged` on a parent that contains active user input forms.
2. **WASM OTel MUST be 'Simple'**: Never use `AddOtlpExporter()` inside signal blocks in WASM projects. Use the cross-cutting `.UseOtlpExporter()` and enforce `ExportProcessorType.Simple`.
3. **Dropdowns Require Placeholders**: A `FluentSelect` without a default placeholder is a "Ghost Selection" trap that will snap back to index 0 upon re-render.
