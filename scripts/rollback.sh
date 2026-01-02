#!/bin/bash

# Rollback Script for Chanzup Platform
# Usage: ./rollback.sh [environment] [version]

set -e

ENVIRONMENT=${1:-staging}
TARGET_VERSION=${2}
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log() {
    echo -e "${GREEN}[ROLLBACK] $1${NC}"
}

warn() {
    echo -e "${YELLOW}[ROLLBACK] WARNING: $1${NC}"
}

error() {
    echo -e "${RED}[ROLLBACK] ERROR: $1${NC}"
    exit 1
}

# Load environment configuration
ENV_FILE="$PROJECT_ROOT/.env.$ENVIRONMENT"
if [[ ! -f "$ENV_FILE" ]]; then
    error "Environment file $ENV_FILE not found"
fi

source "$ENV_FILE"

# Get current deployment state
CURRENT_VERSION=""
CURRENT_ENV=""

if [[ -f "$PROJECT_ROOT/.current-version" ]]; then
    CURRENT_VERSION=$(cat "$PROJECT_ROOT/.current-version")
fi

if [[ -f "$PROJECT_ROOT/.current-environment" ]]; then
    CURRENT_ENV=$(cat "$PROJECT_ROOT/.current-environment")
fi

log "Current version: ${CURRENT_VERSION:-unknown}"
log "Current environment: ${CURRENT_ENV:-unknown}"

# If no target version specified, try to find previous version
if [[ -z "$TARGET_VERSION" ]]; then
    if [[ -f "$PROJECT_ROOT/.deployment-history" ]]; then
        # Get the second most recent version from history
        TARGET_VERSION=$(tail -n 2 "$PROJECT_ROOT/.deployment-history" | head -n 1 | cut -d' ' -f1)
    fi
    
    if [[ -z "$TARGET_VERSION" ]]; then
        error "No target version specified and no deployment history found. Usage: ./rollback.sh $ENVIRONMENT <version>"
    fi
fi

log "Rolling back to version: $TARGET_VERSION"

# Confirm rollback
warn "This will rollback the application to version $TARGET_VERSION"
warn "Current version $CURRENT_VERSION will be stopped"
echo -n "Are you sure you want to proceed? (y/N): "
read -r confirmation

if [[ "$confirmation" != "y" && "$confirmation" != "Y" ]]; then
    log "Rollback cancelled"
    exit 0
fi

# Check if target version image exists
TARGET_IMAGE="ghcr.io/chanzup/platform-api:$TARGET_VERSION"
if ! docker image inspect "$TARGET_IMAGE" > /dev/null 2>&1; then
    log "Target image not found locally, pulling..."
    docker pull "$TARGET_IMAGE" || error "Failed to pull target image $TARGET_IMAGE"
fi

# Determine target environment (opposite of current for blue-green)
TARGET_ENV="blue"
if [[ "$CURRENT_ENV" == "blue" ]]; then
    TARGET_ENV="green"
fi

log "Rolling back to $TARGET_ENV environment with version $TARGET_VERSION"

# Create rollback compose file
cat > "$PROJECT_ROOT/docker-compose.$ENVIRONMENT-rollback.yml" << EOF
version: '3.8'

services:
  api-rollback:
    image: $TARGET_IMAGE
    ports:
      - "8090:8080"  # Use different port for rollback
    environment:
      - ASPNETCORE_ENVIRONMENT=$ENVIRONMENT
      - DATABASE_CONNECTION_STRING=\${DATABASE_CONNECTION_STRING}
      - REDIS_CONNECTION_STRING=\${REDIS_CONNECTION_STRING}
      - JWT_SECRET_KEY=\${JWT_SECRET_KEY}
      - JWT_ISSUER=\${JWT_ISSUER}
      - JWT_AUDIENCE=\${JWT_AUDIENCE}
    depends_on:
      - redis-rollback
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  redis-rollback:
    image: redis:7-alpine
    ports:
      - "6390:6379"  # Use different port for rollback
    command: redis-server --appendonly yes
    volumes:
      - redis_rollback_data:/data
    restart: unless-stopped

volumes:
  redis_rollback_data:
EOF

# Start rollback environment
log "Starting rollback environment..."
cd "$PROJECT_ROOT"

export BUILD_VERSION="$TARGET_VERSION"
export ENVIRONMENT

docker-compose -f "docker-compose.$ENVIRONMENT-rollback.yml" up -d

# Wait for rollback environment to be healthy
log "Waiting for rollback environment to be healthy..."
timeout=300
elapsed=0
interval=10

while [[ $elapsed -lt $timeout ]]; do
    if curl -f -s "http://localhost:8090/health" > /dev/null; then
        log "Rollback environment is healthy!"
        break
    fi
    
    log "Waiting for rollback environment to start... ($elapsed/$timeout seconds)"
    sleep $interval
    elapsed=$((elapsed + interval))
done

if [[ $elapsed -ge $timeout ]]; then
    error "Rollback environment failed to start within $timeout seconds"
fi

# Run smoke tests on rollback environment
log "Running smoke tests on rollback environment..."
BASE_URL="http://localhost:8090" "$SCRIPT_DIR/smoke-tests.sh" "$ENVIRONMENT" || error "Smoke tests failed on rollback environment"

# Database rollback (if needed)
if [[ -n "$3" && "$3" == "--with-db-rollback" ]]; then
    warn "Database rollback requested"
    echo -n "Enter the migration name to rollback to: "
    read -r migration_name
    
    if [[ -n "$migration_name" ]]; then
        log "Rolling back database to migration: $migration_name"
        "$SCRIPT_DIR/db-migrate.sh" "$ENVIRONMENT" rollback "$migration_name" || error "Database rollback failed"
    fi
fi

# Switch traffic to rollback environment
log "Switching traffic to rollback environment..."

# Update nginx configuration
cat > "$PROJECT_ROOT/nginx-$ENVIRONMENT.conf" << EOF
upstream api {
    server localhost:8090;
}
EOF

# Reload nginx
if command -v nginx > /dev/null; then
    nginx -s reload || warn "Failed to reload nginx configuration"
fi

# Wait for traffic to switch
sleep 10

# Verify rollback environment is receiving traffic
log "Verifying traffic switch..."
for i in {1..5}; do
    if curl -f -s "http://localhost:8090/health" > /dev/null; then
        log "✅ Traffic verification $i/5 successful"
    else
        error "❌ Traffic verification failed"
    fi
    sleep 2
done

# Stop current environment
if [[ -n "$CURRENT_ENV" && "$CURRENT_ENV" != "none" ]]; then
    log "Stopping current environment: $CURRENT_ENV"
    docker-compose -f "docker-compose.$ENVIRONMENT-$CURRENT_ENV.yml" down || warn "Failed to stop current environment"
fi

# Update deployment state
echo "rollback" > "$PROJECT_ROOT/.current-environment"
echo "$TARGET_VERSION" > "$PROJECT_ROOT/.current-version"
echo "$(date) - Rollback to $TARGET_VERSION from $CURRENT_VERSION" >> "$PROJECT_ROOT/.rollback-history"

log "Rollback completed successfully!"
log "Application is now running version: $TARGET_VERSION"
log "Application URL: http://localhost:8090"
log "Health check: http://localhost:8090/health"

# Send notification
if [[ -n "$SLACK_WEBHOOK_URL" ]]; then
    curl -X POST -H 'Content-type: application/json' \
        --data "{\"text\":\"⚠️ Chanzup Platform rolled back from $CURRENT_VERSION to $TARGET_VERSION in $ENVIRONMENT\"}" \
        "$SLACK_WEBHOOK_URL" || warn "Failed to send Slack notification"
fi

log "Rollback completed. Monitor the application closely."
log "To rollback the database, run: ./db-migrate.sh $ENVIRONMENT rollback <migration-name>"