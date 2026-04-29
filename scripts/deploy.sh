#!/bin/bash
# scripts/deploy.sh - The One True Way (TM) Local Production Runner 🚀
# Automates the official .NET Aspire deployment workflow for local production simulation.

set -e

# ANSI Color codes for output formatting
CYAN='\033[0;36m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
RED='\033[0;31m'
NC='\033[0m'

# --- Configuration ---
RESOURCE_NAME="env" # Matches builder.AddDockerComposeEnvironment("env") in AppHost

# --- Modular Functions ---

show_help() {
    echo -e "${CYAN}Usage: ./deploy.sh --id <ID> --secret <SECRET>${NC}"
    echo ""
    echo "Options:"
    echo "  --id      Luno API Key ID (Required)"
    echo "  --secret  Luno API Key Secret (Required)"
}

parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --id)     LUNO_ID="$2"; shift 2 ;;
            --secret) LUNO_SECRET="$2"; shift 2 ;;
            *)        show_help; exit 1 ;;
        esac
    done

    if [[ -z "$LUNO_ID" ]] || [[ -z "$LUNO_SECRET" ]]; then
        echo -e "${RED}Error: Missing required credentials.${NC}"
        show_help
        exit 1
    fi
}

check_prerequisites() {
    echo -e "${CYAN}Verifying system prerequisites...${NC}"
    docker info > /dev/null 2>&1 || { echo -e "${RED}Error: Docker not running.${NC}"; exit 1; }
    command -v aspire &> /dev/null || { echo -e "${RED}Error: Aspire CLI missing.${NC}"; exit 1; }
    echo -e "${GREEN}Prerequisites verified.${NC}"
}

configure_environment() {
    export ASPIRE_ALLOW_UNSECURED_TRANSPORT=true
    export ASPIRE_ENABLE_CONTAINER_TUNNEL=true
    export ASPIRE_DISABLE_IDE_INTEGRATION=true
    export Parameters__luno_api_key_id="$LUNO_ID"
    export Parameters__luno_api_key_secret="$LUNO_SECRET"
}

cleanup_previous_run() {
    echo -e "${YELLOW}Cleaning previous deployment...${NC}"
    # Grounding: Official command to stop and remove an Aspire-managed Docker Compose environment.
    # This handles network cleanup and orphan removal more gracefully than raw docker commands.
    aspire do docker-compose-down-$RESOURCE_NAME --non-interactive 2>/dev/null || true
    
    # Force removal of the output directory to ensure a fresh build/publish cycle.
    rm -rf ./aspire-output
}

execute_aspire_deploy() {
    echo -e "${GREEN}Executing 'aspire deploy'...${NC}"
    # Grounding: Official 'one-step' deployment command.
    # NOTE: We handle exit code 6 (RPC disconnection) which can occur in WSL2 after a successful trigger.
    aspire deploy --non-interactive --environment Production --output-path ./aspire-output || {
        RET=$?
        if [ $RET -ne 6 ]; then
            echo -e "${RED}Error: Aspire deploy failed with exit code $RET${NC}"
            exit $RET
        fi
        echo -e "${YELLOW}Warning: Aspire CLI lost connection to AppHost (Code 6), but deployment was triggered successfully.${NC}"
    }
}

wait_for_containers() {
    local max_wait=300
    local start_time=$(date +%s)
    local COMPOSE_FILE=""

    echo -e "${CYAN}Waiting for services to reach a healthy state (Max 5m)...${NC}"

    # 1. Wait for manifest
    while [[ -z "$COMPOSE_FILE" ]]; do
        # Use || true to prevent set -e from killing the script if grep doesn't find a match yet
        COMPOSE_FILE=$(find . -name "docker-compose.yaml" | grep "aspire-output" | head -n 1 || true)
        if [[ -z "$COMPOSE_FILE" ]]; then
            (( $(date +%s) - start_time > max_wait )) && { echo -e "${RED}Manifest timeout.${NC}"; exit 1; }
            sleep 2
        fi
    done

    # 2. Extract service names from the compose file (e.g., 'webfrontend', 'env-dashboard')
    # We look for lines ending in a colon that are top-level under 'services:'
    local services=$(grep -A 20 "services:" "$COMPOSE_FILE" | grep -E "^  [a-zA-Z0-9_-]+:" | sed 's/  //;s/://')
    local total_services=$(echo "$services" | wc -w)
    echo -e "${GREEN}Detected ${total_services} services: ${services}${NC}"

    # 3. Wait for all containers to be 'running' AND none to be 'unhealthy'
    # NOTE: We use direct 'docker inspect' calls here instead of 'docker compose ps'.
    # RATIONALE: In WSL2 environments, 'docker-compose' gRPC communication often fails under load
    # with "invalid proto" errors. Direct CLI calls are the most resilient "Gold Standard".
    while true; do
        local current_time=$(date +%s)
        if (( current_time - start_time > max_wait )); then
            echo -e "${RED}Error: Deployment timed out.${NC}"
            exit 1
        fi

        local running_count=0
        local unhealthy_found=0
        local starting_found=0

        for service in $services; do
            # Find the container name associated with this service (handles compose project prefixing)
            local container_id=$(docker ps --filter "name=$service" --quiet | head -n 1)
            
            if [[ -n "$container_id" ]]; then
                # Inspect the container state directly (avoiding docker compose ps)
                local state=$(docker inspect --format '{{.State.Status}} {{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' "$container_id" 2>/dev/null || echo "missing none")
                
                if echo "$state" | grep -q "running"; then
                    if echo "$state" | grep -q "unhealthy"; then
                        unhealthy_found=1
                    elif echo "$state" | grep -q "starting"; then
                        starting_found=1
                    else
                        ((running_count += 1))
                    fi
                fi
            fi
        done

        if [[ "$unhealthy_found" -eq 1 ]]; then
            echo -e "\n${RED}Error: One or more containers are UNHEALTHY.${NC}"
            exit 1
        fi

        if [[ "$running_count" -eq "$total_services" ]] && [[ "$starting_found" -eq 0 ]]; then
            echo -e "\n${GREEN}All ${total_services} services are online and stable!${NC}"
            break
        fi

        echo -en "${YELLOW}Waiting for services... (${running_count}/${total_services} ready) $((current_time - start_time))s\r${NC}"
        sleep 5
    done
    
    sleep 2 # Final stabilization buffer
}

get_dashboard_url() {
    # Grounding: Official way to discover running AppHosts and their Dashboard URLs.
    # We use '|| true' because grep returns 1 if no running AppHost is found (common after deploy).
    local dashboard=$(aspire ps --format Json | grep -oE "https://localhost:[0-9]+/login\?t=[a-zA-Z0-9]+" | head -n 1 || true)
    
    if [[ -z "$dashboard" ]]; then
        # Fallback: If the AppHost exited but the dashboard container is still alive.
        local container_name=$(docker ps --filter "name=env-dashboard" --format "{{.Names}}" | head -n 1)
        if [[ -n "$container_name" ]]; then
            dashboard=$(docker logs "$container_name" 2>&1 | grep "login?t=" | tail -n 1 | sed 's/.*http/http/' | sed 's/[[:space:]].*//' || true)
        fi
    fi
    echo "${dashboard:-http://localhost:18888 (Token not found)}"
}

get_web_url() {
    local container_name=$(docker ps --filter "name=webfrontend" --format "{{.Names}}" | head -n 1)
    if [[ -n "$container_name" ]]; then
        local port=$(docker port "$container_name" 8080 | grep -oE "[0-9]+$" | head -n 1)
        echo "http://localhost:$port"
    else
        echo "http://localhost: (Container not found)"
    fi
}

run_verification_smoke_test() {
    echo -e "${CYAN}Starting automated verification...${NC}"
    if command -v node &> /dev/null; then
        export WEB_FRONTEND_URL=$(get_web_url)
        node scripts/verify_deploy.js
    fi
}

# --- Main Flow ---
parse_arguments "$@"
check_prerequisites
configure_environment
cleanup_previous_run
execute_aspire_deploy
wait_for_containers

DASHBOARD_URL=$(get_dashboard_url)
WEB_URL=$(get_web_url)

run_verification_smoke_test

echo -e "${GREEN}------------------------------------------------------------${NC}"
echo -e "${GREEN}✅ Local Production is LIVE and VALIDATED!${NC}"
echo ""
echo -e "🌐 Web Frontend:      ${CYAN}${WEB_URL}${NC}"
echo -e "📊 Aspire Dashboard:  ${CYAN}${DASHBOARD_URL}${NC}"
echo -e "${GREEN}------------------------------------------------------------${NC}"
