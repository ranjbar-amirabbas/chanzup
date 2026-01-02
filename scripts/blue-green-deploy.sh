#!/bin/bash

# Blue-Green Deployment Script for Chanzup Platform
# Usage: ./blue-green-deploy.sh [environment] [version]

set -e

ENVIRONMENT=${1:-staging}
VERSION=${2:-latest}
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log() {
    echo -e "${GREEN}[BLUE-GREEN] $1${NC}"
}

warn() {
    echo -e "${YELLOW}[BLUE-GREEN] WARNING: $1${NC}"
}

error() {
    echo -e "${RED}[BLUE-GREEN] ERROR: $1${NC}"
    exit 1
}

blue() {
    echo -e "${BLUE}[BLUE-GREEN] $1${NC}"
}

# Load environment configuration
ENV_FILE="$PROJECT_ROOT/.env.$ENVIRONMENT"
if [[ ! -f "$ENV_FILE" ]]; then
    error "Environment file $ENV_FILE not found"
fi

source "$ENV_FILE"

# Determine current and new environments
CURRENT_ENV=""
NEW_ENV=""

# Check which environment is currently active
if docker-compose -f "$PROJECT_ROOT/docker-compose.$ENVIRONMENT-blue.yml" ps -q > /dev/null 2>&1; then
    if [[ $(docker-compose -f "$PROJECT_ROOT/docker-compose.$ENVIRONMENT-blue.yml" ps -q | wc -l) -gt 0 ]]; then
        CURRENT_ENV="blue"
        NEW_ENV="green"
    fi
fi

if docker-compose -f "$PROJECT_ROOT/docker-compose.$ENVIRONMENT-green.yml" ps -q > /dev/null 2>&1; then
    if [[ $(docker-compose -f "$PROJECT_ROOT/docker-compose.$ENVIRONMENT-green.yml" ps -q | wc -l) -gt 0 ]]; then
        if [[ -n "$CURRENT_ENV" ]]; then
            error "Both blue and green environments are running"
        fi
        CURRENT_ENV="green"
        NEW_ENV="blue"
    fi
fi

# If no environment is running, start with blue
if [[ -z "$CURRENT_ENV" ]]; then
    CURRENT_ENV="none"
    NEW_ENV="blue"
fi

log "Current environment: $CURRENT_ENV"
log "Deploying to: $NEW_ENV"
log "Version: $VERSION"

# Create blue-green compose files if they don't exist
create_compose_files() {
    local color="$1"
    local port_offset="$2"
    
    cat > "$PROJECT_ROOT/docker-compose.$ENVIRONMENT-$color.yml" << EOF
version: '3.8'

services:
  api-$color:
    image: ghcr.io/chanzup/platform-api:$VERSION
    ports:
      - "$((8080 + port_offset)):8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=$ENVIRONMENT
      - DATABASE_CONNECTION_STRING=\${DATABASE_CONNECTION_STRING}
      - REDIS_CONNECTION_STRING=\${REDIS_CONNECTION_STRING}
      - JWT_SECRET_KEY=\${JWT_SECRET_KEY}
      - JWT_ISSUER=\${JWT_ISSUER}
      - JWT_AUDIENCE=\${JWT_AUDIENCE}
    depends_on:
      - redis-$color
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  redis-$color:
    image: redis:7-alpine
    ports:
      - "$((6379 + port_offset)):6379"
    command: redis-server --appendonly yes
    volumes:
      - redis_${color}_data:/data
    restart: unless-stopped

volumes:
  redis_${color}_data:
EOF
}

# Create compose files for blue-green deployment
if [[ "$NEW_ENV" == "blue" ]]; then
    create_compose_files "blue" 0
else
    create_compose_files "green" 10
fi

# Deploy to new environment
log "Deploying to $NEW_ENV environment..."
cd "$PROJECT_ROOT"

export BUILD_VERSION="$VERSION"
export ENVIRONMENT

# Start new environment
docker-compose -f "docker-compose.$ENVIRONMENT-$NEW_ENV.yml" up -d

# Wait for new environment to be healthy
log "Waiting for $NEW_ENV environment to be healthy..."
NEW_PORT=$((8080 + (NEW_ENV == "green" ? 10 : 0)))
timeout=300
elapsed=0
interval=10

while [[ $elapsed -lt $timeout ]]; do
    if curl -f -s "http://localhost:$NEW_PORT/health" > /dev/null; then
        log "$NEW_ENV environment is healthy!"
        break
    fi
    
    log "Waiting for $NEW_ENV environment to start... ($elapsed/$timeout seconds)"
    sleep $interval
    elapsed=$((elapsed + interval))
done

if [[ $elapsed -ge $timeout ]]; then
    error "$NEW_ENV environment failed to start within $timeout seconds"
fi

# Run smoke tests on new environment
log "Running smoke tests on $NEW_ENV environment..."
BASE_URL="http://localhost:$NEW_PORT" "$SCRIPT_DIR/smoke-tests.sh" "$ENVIRONMENT" || error "Smoke tests failed on $NEW_ENV environment"

# Switch traffic to new environment (update load balancer configuration)
log "Switching traffic to $NEW_ENV environment..."

# Update nginx configuration to point to new environment
cat > "$PROJECT_ROOT/nginx-$ENVIRONMENT.conf" << EOF
upstream api {
    server localhost:$NEW_PORT;
}
EOF

# Reload nginx (this would be done by your load balancer/reverse proxy)
if command -v nginx > /dev/null; then
    nginx -s reload || warn "Failed to reload nginx configuration"
fi

# Wait a bit for traffic to switch
sleep 10

# Verify new environment is receiving traffic
log "Verifying traffic switch..."
for i in {1..5}; do
    if curl -f -s "http://localhost:$NEW_PORT/health" > /dev/null; then
        log "✅ Traffic verification $i/5 successful"
    else
        error "❌ Traffic verification failed"
    fi
    sleep 2
done

# Stop old environment
if [[ "$CURRENT_ENV" != "none" ]]; then
    log "Stopping $CURRENT_ENV environment..."
    docker-compose -f "docker-compose.$ENVIRONMENT-$CURRENT_ENV.yml" down
    
    # Clean up old environment resources
    log "Cleaning up $CURRENT_ENV environment resources..."
    docker volume rm "${PROJECT_ROOT##*/}_redis_${CURRENT_ENV}_data" 2>/dev/null || true
fi

# Clean up old images
log "Cleaning up old Docker images..."
docker image prune -f

log "Blue-green deployment completed successfully!"
log "Active environment: $NEW_ENV"
log "Application is running at: http://localhost:$NEW_PORT"
log "Health check: http://localhost:$NEW_PORT/health"

# Send notification
if [[ -n "$SLACK_WEBHOOK_URL" ]]; then
    curl -X POST -H 'Content-type: application/json' \
        --data "{\"text\":\"🔄 Chanzup Platform blue-green deployment completed: $CURRENT_ENV → $NEW_ENV (version: $VERSION)\"}" \
        "$SLACK_WEBHOOK_URL" || warn "Failed to send Slack notification"
fi

# Save deployment state
echo "$NEW_ENV" > "$PROJECT_ROOT/.current-environment"
echo "$VERSION" > "$PROJECT_ROOT/.current-version"
echo "$(date)" > "$PROJECT_ROOT/.last-deployment"

log "Deployment state saved"
log "Current environment: $NEW_ENV"
log "Current version: $VERSION"