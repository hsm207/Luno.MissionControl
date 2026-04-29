#!/bin/bash
# scripts/deploy.sh - Local Production Runner (Aspire Edition) 🚀
# Automates the injection of Luno credentials and launches the Aspire AppHost in Production mode.

set -e

# ANSI Color codes for output formatting
CYAN='\033[0;36m'
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m'

# 1. Help Function
show_help() {
    echo -e "${CYAN}Usage: ./deploy.sh --id <ID> --secret <SECRET>${NC}"
    echo ""
    echo "Options:"
    echo "  --id      Luno API Key ID (Required)"
    echo "  --secret  Luno API Key Secret (Required)"
    echo ""
    echo "Note: This script launches the Aspire AppHost in LIVE production mode."
}

# 2. Parse Arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --id)     export Luno__ApiKeyId="$2"; shift 2 ;;
    --secret) export Luno__ApiKeySecret="$2"; shift 2 ;;
    *)        show_help; exit 1 ;;
  esac
done

# 3. Validation
if [ -z "$Luno__ApiKeyId" ] || [ -z "$Luno__ApiKeySecret" ]; then
    echo -e "${RED}Error: Missing required credentials (API ID or Secret).${NC}"
    show_help
    exit 1
fi

# 4. Set Production Environment
# Note: We set environment for both ASPNETCORE and DOTNET to ensure AppHost and Services are aligned.
export ASPNETCORE_ENVIRONMENT=Production
export DOTNET_ENVIRONMENT=Production

echo -e "${CYAN}Initializing local production execution via .NET Aspire...${NC}"

# 5. Clean & Run AppHost
echo -e "${GREEN}Cleaning existing build artifacts...${NC}"
dotnet clean Luno.MissionControl.slnx

echo -e "${GREEN}Launching Aspire AppHost in production mode (Real Trading Enabled)...${NC}"
dotnet run --project Luno.MissionControl.AppHost/Luno.MissionControl.AppHost.csproj -c Release --no-launch-profile
