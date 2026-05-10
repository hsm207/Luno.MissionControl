#!/bin/bash
# scripts/deploy.sh - Executive Production Orchestrator 🚀

set -e

# --- Configuration & Constants ---
PROJECT_NAME="Luno.MissionControl"
OUTPUT_DIR="./aspire-output"
COMPOSE_FILE="$OUTPUT_DIR/docker-compose.yaml"

WEB_INTERNAL_PORT=8080
DASHBOARD_INTERNAL_PORT=18888

GREEN='\033[0;32m'
CYAN='\033[0;36m'
YELLOW='\033[0;33m'
RED='\033[1;31m'
BOLD='\033[1m'
NC='\033[0m'

# --- 1. Infrastructure Functions ---

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

purge_existing_deployments() {
    echo -e "${YELLOW}🧹 Purging existing environment...${NC}"

    # Absolute Scour: Kill ANY container that belongs to a compose project or has our service names
    docker ps -aq --filter "label=com.docker.compose.project" | xargs -r docker rm -f || true
    docker network prune -f --filter "label=com.docker.compose.project" 2>/dev/null || true
    
    sleep 1

    rm -rf "$OUTPUT_DIR"
    mkdir -p "$OUTPUT_DIR"
}

trigger_aspire_deployment() {
    echo -e "${YELLOW}🏗️  Deploying Aspire Resources (Detached)...${NC}"
    
    export Luno__ApiKeyId="$LUNO_ID"
    export Luno__ApiKeySecret="$LUNO_SECRET"
    export ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL="http://env-dashboard:18890"
    
    aspire deploy --non-interactive --environment Production --output-path "$OUTPUT_DIR" || {
        local ret=$?
        [ $ret -eq 6 ] && echo -e "${YELLOW}⚠️  Note: RPC disconnected (Code 6), but deployment triggered.${NC}" || exit $ret
    }
}

# --- 2. Orchestration Helpers ---

wait_for_container_ready() {
    local service=$1
    local timeout=600 # 10 minutes
    local elapsed=0
    
    echo -ne "   ⏳ Waiting for '$service' container... "
    while [ $elapsed -lt $timeout ]; do
        if docker ps --filter "label=com.docker.compose.service=$service" --filter "status=running" --quiet | grep -q .; then
            echo -e "${GREEN}Running!${NC}"
            return 0
        fi
        sleep 2
        ((elapsed+=2))
    done
    echo -e "${RED}Timeout!${NC}"
    return 1
}

discover_service_url() {
    local service=$1
    local container_port=$2
    local url=""
    
    local container_id=$(docker ps --filter "label=com.docker.compose.service=$service" --filter "status=running" --quiet | head -n 1)
    
    if [ -n "$container_id" ]; then
        local host_port=$(docker port "$container_id" "$container_port" | grep -oE "[0-9]+$" | head -n 1)
        [ -n "$host_port" ] && url="http://localhost:$host_port"
    fi
    
    echo "$url"
}

discover_dashboard_token() {
    local service="env-dashboard"
    local container_id=$(docker ps --filter "label=com.docker.compose.service=$service" --filter "status=running" --quiet | head -n 1)
    
    if [ -n "$container_id" ]; then
        local token=$(docker logs "$container_id" 2>&1 | grep "login?t=" | tail -n 1 | grep -oE "t=[a-zA-Z0-9]+" | cut -d'=' -f2)
        echo "$token"
    fi
}

wait_for_dashboard_token() {
    local timeout=300 # 5 minutes
    local elapsed=0
    
    echo -ne "   ⏳ Waiting for dashboard token... " >&2
    while [ $elapsed -lt $timeout ]; do
        local token=$(discover_dashboard_token)
        if [ -n "$token" ]; then
            echo -e "${GREEN}Discovered!${NC}" >&2
            echo "$token"
            return 0
        fi
        sleep 5
        ((elapsed+=5))
    done
    echo -e "${RED}Timeout!${NC}" >&2
    return 1
}

dump_diagnostic_logs() {
    echo -e "${RED}${BOLD}❌ MISSION CONTROL DEPLOYMENT FAILED HEALTH CHECKS!${NC}"
    echo -e "${YELLOW}📜 DUMPING LOGS FOR DIAGNOSTICS:${NC}"
    echo "------------------------------------------------------------"
    docker ps -a --filter "label=com.docker.compose.project" --format '{{.Names}}' | xargs -I {} sh -c "echo '--- LOGS FOR {}: ---'; docker logs {}; echo ''"
    echo "------------------------------------------------------------"
}

# --- 3. Main Mission Control Orchestrator ---

main() {
    parse_arguments "$@"

    echo -e "${CYAN}${BOLD}🚀 MISSION CONTROL: PRODUCTION DEPLOYMENT STARTING${NC}"
    echo "------------------------------------------------------------"

    purge_existing_deployments

    trigger_aspire_deployment

    echo -e "${YELLOW}📡 Synchronizing with Docker Engine...${NC}"
    wait_for_container_ready "webfrontend"
    wait_for_container_ready "env-dashboard"

    echo -e "${YELLOW}🔑 Discovering Service Endpoints (Docker Native)...${NC}"
    local frontend_url=$(discover_service_url "webfrontend" "$WEB_INTERNAL_PORT")
    local dashboard_base=$(discover_service_url "env-dashboard" "$DASHBOARD_INTERNAL_PORT")
    
    local dashboard_token=$(wait_for_dashboard_token | tail -n 1)

    if [ -z "$dashboard_token" ]; then
        echo -e "${RED}❌ Dashboard token discovery failed or timed out! Observability is required for deployment.${NC}"
        exit 1
    fi

    if [ -z "$frontend_url" ]; then
        echo -e "${RED}❌ Could not discover Web Frontend URL!${NC}"
        exit 1
    fi

    echo -e "${YELLOW}🧪 Verifying Application Health (HTTP)...${NC}"
    if node scripts/verify_deploy.js "$frontend_url"; then
        echo "------------------------------------------------------------"
        echo -e "${GREEN}${BOLD}✨ MISSION CONTROL DEPLOYMENT SUCCESSFUL!${NC}"
        if [ -n "$dashboard_base" ] && [ -n "$dashboard_token" ]; then
            echo -e "   🌐 Dashboard: ${BOLD}$dashboard_base/login?t=$dashboard_token${NC}"
        elif [ -n "$dashboard_base" ]; then
            echo -e "   🌐 Dashboard: ${BOLD}$dashboard_base${NC}"
        fi
        echo -e "   🚀 Frontend:  ${BOLD}$frontend_url${NC}"
    else
        dump_diagnostic_logs
        exit 1
    fi
}

main "$@"
