#!/bin/bash

# Database Migration Script for Chanzup Platform
# Usage: ./db-migrate.sh [environment] [action]
# Actions: migrate, rollback, seed, reset

set -e

ENVIRONMENT=${1:-staging}
ACTION=${2:-migrate}
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log() {
    echo -e "${GREEN}[DB MIGRATION] $1${NC}"
}

warn() {
    echo -e "${YELLOW}[DB MIGRATION] WARNING: $1${NC}"
}

error() {
    echo -e "${RED}[DB MIGRATION] ERROR: $1${NC}"
    exit 1
}

# Validate environment
if [[ ! "$ENVIRONMENT" =~ ^(development|staging|production)$ ]]; then
    error "Invalid environment. Use 'development', 'staging', or 'production'"
fi

# Validate action
if [[ ! "$ACTION" =~ ^(migrate|rollback|seed|reset|status)$ ]]; then
    error "Invalid action. Use 'migrate', 'rollback', 'seed', 'reset', or 'status'"
fi

# Load environment-specific configuration
ENV_FILE="$PROJECT_ROOT/.env.$ENVIRONMENT"
if [[ ! -f "$ENV_FILE" ]]; then
    error "Environment file $ENV_FILE not found"
fi

source "$ENV_FILE"

# Validate required environment variables
if [[ -z "$DATABASE_CONNECTION_STRING" ]]; then
    error "DATABASE_CONNECTION_STRING is not set"
fi

log "Starting database $ACTION for $ENVIRONMENT environment"

# Change to the API project directory
cd "$PROJECT_ROOT/src/Chanzup.API"

case "$ACTION" in
    "migrate")
        log "Applying database migrations..."
        dotnet ef database update \
            --connection "$DATABASE_CONNECTION_STRING" \
            --verbose || error "Migration failed"
        log "Database migration completed successfully"
        ;;
    
    "rollback")
        if [[ -z "$3" ]]; then
            error "Rollback requires a migration name. Usage: ./db-migrate.sh $ENVIRONMENT rollback <migration-name>"
        fi
        MIGRATION_NAME="$3"
        log "Rolling back to migration: $MIGRATION_NAME"
        dotnet ef database update "$MIGRATION_NAME" \
            --connection "$DATABASE_CONNECTION_STRING" \
            --verbose || error "Rollback failed"
        log "Database rollback completed successfully"
        ;;
    
    "seed")
        log "Seeding database with initial data..."
        # Run the seeder
        dotnet run --no-build -- --seed \
            --connection "$DATABASE_CONNECTION_STRING" || error "Database seeding failed"
        log "Database seeding completed successfully"
        ;;
    
    "reset")
        if [[ "$ENVIRONMENT" == "production" ]]; then
            error "Database reset is not allowed in production environment"
        fi
        
        warn "This will delete all data in the database. Are you sure? (y/N)"
        read -r confirmation
        if [[ "$confirmation" != "y" && "$confirmation" != "Y" ]]; then
            log "Database reset cancelled"
            exit 0
        fi
        
        log "Dropping database..."
        dotnet ef database drop --force \
            --connection "$DATABASE_CONNECTION_STRING" || warn "Database drop failed (may not exist)"
        
        log "Creating and migrating database..."
        dotnet ef database update \
            --connection "$DATABASE_CONNECTION_STRING" \
            --verbose || error "Database creation failed"
        
        log "Seeding database..."
        dotnet run --no-build -- --seed \
            --connection "$DATABASE_CONNECTION_STRING" || error "Database seeding failed"
        
        log "Database reset completed successfully"
        ;;
    
    "status")
        log "Checking database migration status..."
        dotnet ef migrations list \
            --connection "$DATABASE_CONNECTION_STRING" || error "Failed to get migration status"
        ;;
esac

# Verify database connectivity
log "Verifying database connectivity..."
if dotnet run --no-build -- --health-check \
    --connection "$DATABASE_CONNECTION_STRING" > /dev/null 2>&1; then
    log "✅ Database is accessible and healthy"
else
    error "❌ Database connectivity check failed"
fi

log "Database operation completed successfully!"
log "Environment: $ENVIRONMENT"
log "Action: $ACTION"
log "Timestamp: $(date)"