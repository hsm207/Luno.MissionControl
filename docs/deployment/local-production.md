# Local Production Deployment

Runs the full stack locally via Docker Compose using Aspire's `deploy` pipeline.

## Quick Start

```bash
./scripts/deploy.sh --id <LUNO_API_KEY_ID> --secret <LUNO_API_KEY_SECRET>
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://docs.docker.com/engine/install/)
- [Aspire CLI](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling) (`dotnet tool install -g Aspire.Cli`)

## What it does

1. **Purging** — stops and removes any existing Compose-managed containers and networks.
2. **Build** — builds Docker images for `webfrontend` and `settings-migrations`.
3. **Deploy** — runs `aspire deploy` to generate `aspire-output/docker-compose.yaml` and starts the stack.
4. **Health check** — runs `scripts/verify_deploy.js` using Playwright to confirm the frontend serves correctly.

## Services

| Service | Description |
|---------|-------------|
| `webfrontend` | Blazor web app (hosted at a random port, printed on success) |
| `postgres` | PostgreSQL 17 with a persistent named volume |
| `settings-migrations` | One-shot EF Core migrations runner, exits on completion |
| `env-dashboard` | Aspire dashboard (hosted at a random port with login token) |

## Credentials

### Luno API keys (required)
Pass via `--id` and `--secret` arguments. These are injected into the `webfrontend` container as `Luno__ApiKeyId` and `Luno__ApiKeySecret`.

### PostgreSQL password (stable)
The password is managed via a [named parameter](https://aspire.dev/integrations/databases/postgres/postgres-host/#add-postgresql-server-resource-with-parameters) in `Program.cs` that reads from the standard .NET configuration chain. The canonical value is stored as a dotnet user-secret:

```bash
dotnet user-secrets set "Parameters:postgres-password" "<password>" --project Luno.MissionControl.AppHost
```

Aspire persists this value to its deployment state (`~/.aspire/deployments/<hash>/production.json`) and reuses it on subsequent deploys. Once set, the password stays stable across deployments so the persistent data volume `mission-control-postgres-data` remains usable.

> **If the volume already exists with a different password**, reset it:
> 1. Temporarily add a `local all all trust` entry to `pg_hba.conf` inside the container
> 2. Run `ALTER USER postgres PASSWORD '<new-password>';`
> 3. Restore the original `pg_hba.conf`

## Dependencies

The verification script uses [Playwright](https://playwright.dev) for browser-based health checks. Install it with:

```bash
npm install
```

## Architecture

- [C2 Container Diagram](../architecture/C2_Container.md) — shows the service topology