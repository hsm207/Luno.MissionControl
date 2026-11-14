<#
.SYNOPSIS
    Deploys Luno.MissionControl full-stack locally using Aspire and Docker Compose.

.DESCRIPTION
    Native PowerShell orchestrator mirroring scripts/deploy.sh:
    - Purges existing compose containers/networks.
    - Sets Luno credentials in the environment.
    - Executes `aspire deploy` to generate Compose resources and launch the stack.
    - Waits for containers, extracts ports/tokens, and runs health verification.

.PARAMETER Id
    Luno API Key ID (Required).

.PARAMETER Secret
    Luno API Key Secret (Required).

.EXAMPLE
    .\scripts\deploy.ps1 -Id "your_api_id" -Secret "your_api_secret"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, HelpMessage = "Luno API Key ID")]
    [string]$Id,

    [Parameter(Mandatory = $true, HelpMessage = "Luno API Key Secret")]
    [string]$Secret
)

$ErrorActionPreference = "Stop"

# --- Configuration & Constants ---
$OutputDir = "./aspire-output"
$WebInternalPort = 8080
$DashboardInternalPort = 18888

function Purge-ExistingDeployments {
    Write-Host "🧹 Purging existing environment..." -ForegroundColor Yellow

    # Kill and remove any container with docker compose labels
    $containers = docker ps -aq --filter "label=com.docker.compose.project"
    if ($containers) {
        $containers | ForEach-Object { docker rm -f $_ | Out-Null }
    }

    # Prune unused networks created by compose
    docker network prune -f --filter "label=com.docker.compose.project" 2>$null | Out-Null

    Start-Sleep -Seconds 1

    if (Test-Path $OutputDir) {
        Remove-Item -Recurse -Force $OutputDir
    }
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

function Trigger-AspireDeployment {
    Write-Host "🏗️  Deploying Aspire Resources (Detached)..." -ForegroundColor Yellow

    $env:Luno__ApiKeyId = $Id
    $env:Luno__ApiKeySecret = $Secret
    $env:ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL = "http://env-dashboard:18890"

    aspire deploy --non-interactive --environment Production --output-path "$OutputDir"
    $exitCode = $LASTEXITCODE

    if ($exitCode -ne 0) {
        if ($exitCode -eq 6) {
            Write-Host "⚠️  Note: RPC disconnected (Code 6), but deployment triggered." -ForegroundColor Yellow
        } else {
            throw "Aspire deployment failed with exit code $exitCode"
        }
    }
}

function Wait-ForContainerReady {
    param(
        [string]$Service,
        [int]$TimeoutSeconds = 600
    )

    Write-Host -NoNewline "   ⏳ Waiting for '$Service' container... "
    $sw = [System.Diagnostics.Stopwatch]::StartNew()

    while ($sw.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
        $running = docker ps --filter "label=com.docker.compose.service=$Service" --filter "status=running" --quiet
        if ($running) {
            Write-Host "Running!" -ForegroundColor Green
            return $true
        }
        Start-Sleep -Seconds 2
    }

    Write-Host "Timeout!" -ForegroundColor Red
    return $false
}

function Get-ServiceUrl {
    param(
        [string]$Service,
        [int]$ContainerPort
    )

    $containerId = (docker ps --filter "label=com.docker.compose.service=$Service" --filter "status=running" --quiet | Select-Object -First 1)
    if (-not $containerId) {
        return ""
    }

    $portOutput = docker port $containerId $ContainerPort 2>$null
    if ($portOutput) {
        # Matches the published host port at the end of the line (e.g. 0.0.0.0:54321 -> 54321)
        if ($portOutput -match '(?<port>\d+)$') {
            return "http://localhost:$($Matches.port)"
        }
    }

    return ""
}

function Get-DashboardToken {
    $containerId = (docker ps --filter "label=com.docker.compose.service=env-dashboard" --filter "status=running" --quiet | Select-Object -First 1)
    if (-not $containerId) {
        return ""
    }

    $logs = docker logs $containerId 2>&1
    $tokenMatch = $logs | Select-String -Pattern 'login\?t=([a-zA-Z0-9]+)' | Select-Object -Last 1

    if ($tokenMatch -and $tokenMatch.Matches[0].Groups.Count -gt 1) {
        return $tokenMatch.Matches[0].Groups[1].Value
    }

    return ""
}

function Wait-ForDashboardToken {
    param([int]$TimeoutSeconds = 300)

    [Console]::Error.Write("   ⏳ Waiting for dashboard token... ")
    $sw = [System.Diagnostics.Stopwatch]::StartNew()

    while ($sw.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
        $token = Get-DashboardToken
        if ($token) {
            [Console]::Error.WriteLine("Discovered!")
            return $token
        }
        Start-Sleep -Seconds 5
    }

    [Console]::Error.WriteLine("Timeout!")
    return ""
}

function Dump-DiagnosticLogs {
    Write-Host "❌ MISSION CONTROL DEPLOYMENT FAILED HEALTH CHECKS!" -ForegroundColor Red
    Write-Host "📜 DUMPING LOGS FOR DIAGNOSTICS:" -ForegroundColor Yellow
    Write-Host ("-" * 60)

    $names = docker ps -a --filter "label=com.docker.compose.project" --format "{{.Names}}"
    foreach ($name in $names) {
        if (-not [string]::IsNullOrWhiteSpace($name)) {
            Write-Host "--- LOGS FOR $name ---" -ForegroundColor Cyan
            docker logs $name
            Write-Host ""
        }
    }
    Write-Host ("-" * 60)
}

# --- Main Execution ---
Write-Host "🚀 MISSION CONTROL: PRODUCTION DEPLOYMENT STARTING" -ForegroundColor Cyan
Write-Host ("-" * 60)

Purge-ExistingDeployments

Trigger-AspireDeployment

Write-Host "📡 Synchronizing with Docker Engine..." -ForegroundColor Yellow
if (-not (Wait-ForContainerReady "webfrontend")) {
    Dump-DiagnosticLogs
    exit 1
}
if (-not (Wait-ForContainerReady "env-dashboard")) {
    Dump-DiagnosticLogs
    exit 1
}

Write-Host "🔑 Discovering Service Endpoints (Docker Native)..." -ForegroundColor Yellow
$frontendUrl = Get-ServiceUrl -Service "webfrontend" -ContainerPort $WebInternalPort
$dashboardBase = Get-ServiceUrl -Service "env-dashboard" -ContainerPort $DashboardInternalPort

$dashboardToken = Wait-ForDashboardToken
if (-not $dashboardToken) {
    Write-Host "❌ Dashboard token discovery failed or timed out! Observability is required for deployment." -ForegroundColor Red
    exit 1
}

if (-not $frontendUrl) {
    Write-Host "❌ Could not discover Web Frontend URL!" -ForegroundColor Red
    exit 1
}

Write-Host "🧪 Verifying Application Health (HTTP)..." -ForegroundColor Yellow
& node scripts/verify_deploy.js "$frontendUrl"
if ($LASTEXITCODE -eq 0) {
    Write-Host ("-" * 60)
    Write-Host "✨ MISSION CONTROL DEPLOYMENT SUCCESSFUL!" -ForegroundColor Green
    if ($dashboardBase -and $dashboardToken) {
        Write-Host "   🌐 Dashboard: $dashboardBase/login?t=$dashboardToken" -ForegroundColor Green
    } elseif ($dashboardBase) {
        Write-Host "   🌐 Dashboard: $dashboardBase" -ForegroundColor Green
    }
    Write-Host "   🚀 Frontend:  $frontendUrl" -ForegroundColor Green
} else {
    Dump-DiagnosticLogs
    exit 1
}
