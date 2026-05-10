# Persistent Infrastructure and Aspire Orchestration

This document defines the architectural standard for achieving deterministic data persistence and secure orchestration within the Luno Mission Control ecosystem. It addresses the challenges of maintaining state across container lifecycles and the pitfalls of manual environment variable management.

## 1. The Persistence Bridge: Named Volumes

In a containerized environment, data is ephemeral by default. To ensure user preferences (e.g., wallet selections) survive deployment cycles, a persistence bridge is required.

### Named Volumes vs. Bind Mounts
While **Bind Mounts** (mapping a host directory) are common in development, they introduce "Host Path Dependency" and permissions friction.
*   **Best Practice**: Use **Docker Named Volumes**. 
*   **Portability**: Named volumes are managed by the Docker Engine, making the deployment agnostic of the host's file system structure.
*   **Cleanup**: Data persists until the volume is explicitly deleted (`docker volume rm`), protecting against accidental `docker compose down` wipes.

### Implementation Mandate
Persistence must be conditionally applied based on the environment to preserve a "Clean Slate" developer inner-loop.
```csharp
// AppHost/Program.cs
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("mission-control-postgres-data"); // Named volume persistence
```

## 2. Aspire Native Orchestration

.NET Aspire is more than a runner; it is a sophisticated **Secret Orchestrator**. Leveraging its native capabilities is mandatory for maintaining architectural integrity.

### The "Dangerous" Overwrite Trap
A common anti-pattern is manually generating a `.env` file after running `aspire deploy`. This is categorized as an **Architectural Failure** for several reasons:
1.  **Secret Loss**: Aspire automatically generates complex, secure passwords (e.g., `POSTGRES_PASSWORD`). Manual overwrites wipe these values, leading to connection failures.
2.  **Tag Drift**: Aspire generates specific, timestamped image tags for determinism. Manual `.env` files often revert to `:latest`, causing the deployment to run stale or mismatched images.
3.  **Environment Segregation**: Aspire generates environment-specific files (e.g., `.env.Production`). Manual tools often fail to utilize these, leading to "blank variable" warnings in Docker Compose.

### The Standard Workflow
Trust the Aspire CLI to manage the environment.
1.  **Export Secrets**: Export sensitive keys (e.g., `Luno__ApiKeyId`) in the shell.
2.  **Trigger Deploy**: Run `aspire deploy --environment Production`.
3.  **Automatic Capture**: Aspire captures the exported variables and orchestrates them into the generated manifest and `.env.Production` file automatically.

## 3. Forensic Deployment Verification

Deployment is not complete until verified at the protocol level.

1.  **Container Probe**: Use `docker exec <container> env` to verify that secrets have successfully crossed the architectural boundary.
2.  **Volume Audit**: Verify volume attachment via `docker inspect`.
3.  **Health Verification**: The orchestrator (`deploy.sh`) must perform an automated HTTP probe of the public endpoints to confirm the full stack is operational.

> [!IMPORTANT]
> **Zero-Dumbass Rule**: Never manually edit the files in `aspire-output/`. If a configuration change is needed, apply it in the `AppHost` or via environment variables and re-run the deployment orchestrator. The `aspire-output/` directory is a **build artifact** and must be treated as immutable by human hands.
