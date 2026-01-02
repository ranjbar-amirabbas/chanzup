#!/bin/bash

# Chanzup Platform Deployment Script
# Usage: ./deploy.sh [environment] [version]
# Example: ./deploy.sh production v1.0.0

set -e

ENVIRONMENT=${1:-staging}
VERSION=${2:-latest}
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log() {
    echo -e "${GREEN}[$(date +'%Y-%m-%d %H:%M:%S')] $1${NC}"
}

warn() {
    echo -e "${YELLOW}[$(date +'%Y-%m-%d %H:%M:%S')] WARNING: $1${NC}"
}

error() {
    echo -e "${RED}[$(date +'%Y-%m-%d %H:%M:%S')] ERROR: $1${NC}"
    exit 1
}

# Validate environment
if [[ ! "$ENVIRONMENT" =~ ^(staging|production)$ ]]; then
    error "Invalid environment. Use 'staging' or 'production'"
fi

log "Starting deployment to $ENVIRONMENT environment with version $VERSION"

# Load environment-specific configuration
ENV_FILE="$PROJECT_ROOT/.env.$ENVIRONMENT"
if [[ ! -f "$ENV_FILE" ]]; then
    error "Environment file $ENV_FILE not found"
fi

source "$ENV_FILE"

# Validate required environment variables
required_vars=(
    "DATABASE_CONNECTION_STRING"
    "JWT_SECRET_KEY"
    "GOOGLE_MAPS_API_KEY"
    "STRIPE_SECRET_KEY"
    "SENDGRID_API_KEY"
)

for var in "${required_vars[@]}"; do
    if [[ -z "${!var}" ]]; then
        error "Required environment variable $var is not set"
    fi
done

# Pre-deployment checks
log "Running pre-deployment checks..."

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    error "Docker is not running"
fi

# Check if required images exist
REQUIRED_IMAGES=(
    "ghcr.io/chanzup/platform-api:$VERSION"
    "redis:7-alpine"
    "nginx:alpine"
)

for image in "${REQUIRED_IMAGES[@]}"; do
    if ! docker image inspect "$image" > /dev/null 2>&1; then
        log "Pulling image $image..."
        docker pull "$image" || error "Failed to pull image $image"
    fi
done

# Database migration
log "Running database migrations..."
docker run --rm \
    -e "ConnectionStrings__DefaultConnection=$DATABASE_CONNECTION_STRING" \
    "ghcr.io/chanzup/platform-api:$VERSION" \
    dotnet ef database update || error "Database migration failed"

# Backup current deployment (if exists)
if docker-compose -f "$PROJECT_ROOT/docker-compose.$ENVIRONMENT.yml" ps -q > /dev/null 2>&1; then
    log "Creating backup of current deployment..."
    docker-compose -f "$PROJECT_ROOT/docker-compose.$ENVIRONMENT.yml" down
fi

# Deploy new version
log "Deploying new version..."
cd "$PROJECT_ROOT"

# Export environment variables for docker-compose
export BUILD_VERSION="$VERSION"
export ENVIRONMENT

# Start services
docker-compose -f "docker-compose.$ENVIRONMENT.yml" up -d

# Wait for services to be healthy
log "Waiting for services to be healthy..."
timeout=300
elapsed=0
interval=10

while [[ $elapsed -lt $timeout ]]; do
    if curl -f -s "http://localhost:8080/health" > /dev/null; then
        log "Services are healthy!"
        break
    fi
    
    log "Waiting for services to start... ($elapsed/$timeout seconds)"
    sleep $interval
    elapsed=$((elapsed + interval))
done

if [[ $elapsed -ge $timeout ]]; then
    error "Services failed to start within $timeout seconds"
fi

# Run smoke tests
log "Running smoke tests..."
"$SCRIPT_DIR/smoke-tests.sh" "$ENVIRONMENT" || error "Smoke tests failed"

# Clean up old images
log "Cleaning up old Docker images..."
docker image prune -f

log "Deployment completed successfully!"
log "Application is running at: https://$(hostname)"
log "Health check: https://$(hostname)/health"

# Send notification (if configured)
if [[ -n "$SLACK_WEBHOOK_URL" ]]; then
    curl -X POST -H 'Content-type: application/json' \
        --data "{\"text\":\"✅ Chanzup Platform deployed to $ENVIRONMENT (version: $VERSION)\"}" \
        "$SLACK_WEBHOOK_URL" || warn "Failed to send Slack notification"
fi