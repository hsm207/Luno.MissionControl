# Lesson: Aspire Wiring and UI Interactivity Are Explicit, Not Implied

## The Incident

During the stabilization of the Luno.MissionControl dashboard (targeting .NET 10 and Aspire 13), the Basket Architect UI remained completely frozen despite the server running correctly. Buttons did not respond to clicks, prices displayed as "Loading...", and the total allocation never updated. The session consumed multiple turns diagnosing the wrong layers (OTLP configuration, AppHost orchestration, certificate trust) before the actual root cause was identified.

Two compounding failures were present simultaneously:

1. **The Frozen UI**: `BasketArchitect.razor` had no `@rendermode` directive. Blazor silently rendered it as static server-side HTML, meaning zero event handlers were wired up, zero C# callbacks executed on interaction, and the SignalR circuit that `OnAfterRenderAsync` depended on was never established.
2. **The Unhealthy/Silent Telemetry**: The AppHost was missing its `launchSettings.json` and `appsettings.json`. Without proper HTTPS profile configuration and the `linux-dev-certs` CA trust, the DCP component could not complete SSL handshakes for health probes or OTLP export, causing the dashboard to show "Unhealthy" and empty Structured Logs/Traces.

Neither failure produced an actionable error. The result was a UI that appeared to work (it rendered HTML, page loaded) but was completely inert.

## Root Cause Analysis

### Failure 1: Missing `@rendermode` Directive

Blazor Unified in .NET 8+ introduced a new rendering model where **Static SSR is the default**. A component that does not explicitly opt into an interactive render mode will never establish a Blazor circuit or WebAssembly runtime. There is no compiler warning, no runtime exception, and no browser console error. The page simply does not respond to user interaction.

The investigation wasted multiple turns examining:
- The OTLP exporter configuration (correct)
- The `IBasketState.StartAsync()` wiring (correct)
- The service registrations (correct)

None of these were broken. The entire component was inert because it was never told to be interactive. One line, absent.

**The correct fix**: Add `@rendermode InteractiveAuto` (or `InteractiveServer` / `InteractiveWebAssembly` as appropriate) to any component page that requires user interaction.

```razor
@page "/basket"
@rendermode InteractiveAuto
```

`InteractiveAuto` is the canonical choice for components living in the `Web.Client` (WASM) project under an Aspire-hosted Blazor Web App: it uses Server rendering during the initial load (fast time-to-interactive) and transparently switches to WebAssembly once the runtime is downloaded.

### Failure 2: Missing AppHost Configuration and Untrusted SSL on Linux

The `launchSettings.json` was missing from the AppHost, meaning no HTTPS profile, no OTLP endpoint variables, and no MCP endpoint variables were declared. The correct environment for DCP, the health check prober, and the OTLP exporter is **entirely driven by `launchSettings.json`**.

After restoring `launchSettings.json`, a secondary Linux-specific failure remained: `dotnet dev-certs https --trust` writes the certificate to `~/.aspnet/dev-certs/trust/` but **does not update the system OpenSSL CA bundle**. OpenSSL (used by DCP and .NET's `HttpClient`) ignores that directory unless `SSL_CERT_DIR` is set in the environment.

The definitive diagnostic signal came from `dotnet dev-certs https --check --trust -v`:

```
[76] The certificate is not trusted by OpenSSL.
     Ensure that the SSL_CERT_DIR environment variable is set correctly.
```

The correct fix on Linux is the `linux-dev-certs` global tool (explicitly recommended in the ASP.NET Core enforce-HTTPS documentation), which creates a proper CA, installs it into the system trust store via `certutil`, and updates Firefox profiles:

```bash
dotnet tool update -g linux-dev-certs
dotnet linux-dev-certs install
```

### Why It Took So Long

1. **Symptoms were misleading at every layer.** The AppHost crash pointed to orchestration. The "Unhealthy" status pointed to OTLP. The frozen UI pointed to the Blazor circuit. Each false lead consumed investigation turns before the true cause was exposed by direct evidence.
2. **The wrong debugging strategy was applied first.** Guesses were made ("the OTLP exporter is likely the issue") before the tooling was used to observe the actual state. The correct approach — `dotnet dev-certs https --check --trust -v` for SSL, dashboard env-var panel for OTLP, lab project comparison for render mode — was only reached after the user redirected focus.
3. **The render mode failure was completely invisible until directly tested.** A "frozen" component with no error output does not scream "missing render mode directive". It looks like a slow or hung component, not a completely static one. This required the user to step in and remind us that the entire point of the session was to verify button interactivity — which forced us to actually click buttons.

## Actionable Guardrails

1. **Every interactive Blazor page component MUST have an explicit `@rendermode` directive.** There is no fallback, no inference, no "it just works." Static SSR is the default. Interactivity is opt-in. Verify this before any other layer.

2. **Verify interactivity by running `dotnet test`, not by reading code.** The `RenderModeComplianceTests` in `Tests.Integration` statically analyse every `@page` component and fail immediately if `@rendermode` is absent. Run this before opening any browser. Only use `browser_subagent` for final E2E confirmation after the static tests are green.

3. **On Linux, `dotnet dev-certs https --trust` is not sufficient for OpenSSL trust.** Always run `dotnet linux-dev-certs install` after certificate setup on Linux. Confirm with `dotnet dev-certs https --check --trust -v` and verify the output explicitly says "trusted by OpenSSL."

4. **AppHost `launchSettings.json` is load-bearing, not optional.** It is the sole source of truth for ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL, ASPIRE_DASHBOARD_MCP_ENDPOINT_URL, ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL, and the HTTPS profile. If it is missing, every downstream telemetry, health check, and MCP tool will silently fail. Compare against a freshly scaffolded `aspire-starter` project as the ground truth.

5. **Do not assume a cause from a symptom. Use the tools.** The evidence chain for each failure was available immediately via:
   - `dotnet dev-certs https --check --trust -v` (SSL trust state)
   - `mcp_aspire_list_resources` → inspect `healthStatus`, `urls`, and `environment` for any resource without opening a browser
   - `mcp_aspire_list_structured_logs` → verify telemetry is flowing from `webfrontend`
   - `mcp_aspire_list_traces` → verify distributed traces are being exported
   - `grep -rn "@rendermode"` in `Web.Client` (render mode presence)
   
   Applying these before speculating would have reduced the session from multiple hours to under 30 minutes.

6. **Use Aspire MCP tools for dashboard inspection, not `browser_subagent`.** The MCP server exposes `list_resources`, `list_structured_logs`, `list_traces`, and `list_console_logs` — all of which return machine-readable data instantly. Spinning up a browser subagent to navigate the Aspire Dashboard UI for diagnostic information is slow, brittle (login tokens, cert warnings), and completely unnecessary. Reserve `browser_subagent` for testing the actual application UI — not for observability infrastructure.

7. **The Blazor render mode matrix for Aspire-hosted apps:**

   | Component Location | Recommended Render Mode | Reason |
   |---|---|---|
   | `Web` project (Server only) | `@rendermode InteractiveServer` | Runs on server, full access to DI |
   | `Web.Client` project (WASM) | `@rendermode InteractiveAuto` | Server on first load, WASM after download |
   | `Web.Client` with server services via HTTP | `@rendermode InteractiveAuto` | Same as above |
   | Layout / Shell components | None (inherit from page) | Layouts do not set render mode |
   | Static content pages | None (intentional Static SSR) | No interactivity needed |
