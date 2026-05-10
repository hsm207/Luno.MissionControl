# Lesson 05: The AppHost Startup Threshold and Linux SSL Trust

## The Incident

During the stabilization and testing of the `Luno.MissionControl` UI, we attempted to run the Aspire AppHost in the background using the CLI's native `--background` flag (`aspire start --background`). 

The command appeared to succeed, but immediate attempts to interrogate the AppHost (e.g., `aspire ps`, `aspire describe`) failed with:
`No running apphost found. Use 'aspire run' to start one first.`

This led to a cycle of "ghost" deployments and an inability to obtain dynamically assigned endpoints for Playwright E2E tests.

## Root Cause Analysis

### 1. The Build/Visibility Threshold
Unlike standard foreground processes, a backgrounded Aspire AppHost undergoes a complete `dotnet build` and `dotnet restore` cycle before it registers with the Aspire CLI's internal tracker. During this window (approx. 45-60 seconds in complex environments), the AppHost is active but **invisible** to `aspire ps` and `aspire describe`.

### 2. Linux SSL Trust Paradox
On Linux (including WSL2), `dotnet dev-certs https --trust` writes to the user's local store but does **NOT** update the system OpenSSL CA bundle. This prevents DCP and the AppHost from completing SSL handshakes for health probes and OTLP export, resulting in "Unhealthy" status even if the process is alive.

## Actionable Guardrails

1.  **Poll for Readiness, Do Not Assume Death**: When starting an AppHost in the background, implement a polling loop or use `aspire wait` before attempting to read resource endpoints.
    ```bash
    aspire start --background
    # Wait for the build to finish
    aspire wait webfrontend --timeout 120s
    ```
2.  **Explicit Linux SSL Trust**: Always use the `linux-dev-certs` tool on Linux/WSL2 environments to bridge the OpenSSL trust gap.
    ```bash
    dotnet tool update -g linux-dev-certs
    dotnet linux-dev-certs install
    ```
3.  **Audit the Detach Logs**: If an AppHost fails to appear after 2 minutes, read the child process logs in `~/.aspire/logs/`. They are the only source of truth for build-time failures.
    ```bash
    ls -t ~/.aspire/logs/*detach-child*.log | head -n 1 | xargs cat
    ```
