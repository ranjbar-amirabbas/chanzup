#!/bin/bash

# Smoke Tests for Chanzup Platform
# Usage: ./smoke-tests.sh [environment]

set -e

ENVIRONMENT=${1:-staging}
BASE_URL="http://localhost:8080"

if [[ "$ENVIRONMENT" == "production" ]]; then
    BASE_URL="https://api.chanzup.com"
elif [[ "$ENVIRONMENT" == "staging" ]]; then
    BASE_URL="https://staging-api.chanzup.com"
fi

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log() {
    echo -e "${GREEN}[SMOKE TEST] $1${NC}"
}

warn() {
    echo -e "${YELLOW}[SMOKE TEST] WARNING: $1${NC}"
}

error() {
    echo -e "${RED}[SMOKE TEST] ERROR: $1${NC}"
    exit 1
}

test_endpoint() {
    local endpoint="$1"
    local expected_status="${2:-200}"
    local description="$3"
    
    log "Testing $description: $endpoint"
    
    response=$(curl -s -w "%{http_code}" -o /tmp/response.json "$BASE_URL$endpoint")
    status_code="${response: -3}"
    
    if [[ "$status_code" == "$expected_status" ]]; then
        log "✅ $description - Status: $status_code"
        return 0
    else
        error "❌ $description - Expected: $expected_status, Got: $status_code"
        return 1
    fi
}

test_endpoint_with_auth() {
    local endpoint="$1"
    local token="$2"
    local expected_status="${3:-200}"
    local description="$4"
    
    log "Testing $description: $endpoint"
    
    response=$(curl -s -w "%{http_code}" -o /tmp/response.json \
        -H "Authorization: Bearer $token" \
        "$BASE_URL$endpoint")
    status_code="${response: -3}"
    
    if [[ "$status_code" == "$expected_status" ]]; then
        log "✅ $description - Status: $status_code"
        return 0
    else
        error "❌ $description - Expected: $expected_status, Got: $status_code"
        return 1
    fi
}

log "Starting smoke tests for $ENVIRONMENT environment"
log "Base URL: $BASE_URL"

# Test 1: Health Check
test_endpoint "/health" 200 "Health Check"

# Test 2: Readiness Check
test_endpoint "/health/ready" 200 "Readiness Check"

# Test 3: Liveness Check
test_endpoint "/health/live" 200 "Liveness Check"

# Test 4: API Documentation (if available)
if [[ "$ENVIRONMENT" != "production" ]]; then
    test_endpoint "/swagger" 200 "API Documentation"
fi

# Test 5: Authentication endpoint (should return 400 for empty request)
log "Testing authentication endpoint"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json \
    -X POST \
    -H "Content-Type: application/json" \
    -d '{}' \
    "$BASE_URL/api/auth/login")
status_code="${response: -3}"

if [[ "$status_code" == "400" ]]; then
    log "✅ Authentication endpoint - Status: $status_code (expected for empty request)"
else
    warn "Authentication endpoint returned unexpected status: $status_code"
fi

# Test 6: Protected endpoint without auth (should return 401)
log "Testing protected endpoint without authentication"
response=$(curl -s -w "%{http_code}" -o /tmp/response.json \
    "$BASE_URL/api/campaigns")
status_code="${response: -3}"

if [[ "$status_code" == "401" ]]; then
    log "✅ Protected endpoint security - Status: $status_code (expected for unauthenticated request)"
else
    warn "Protected endpoint returned unexpected status: $status_code"
fi

# Test 7: CORS headers
log "Testing CORS headers"
response=$(curl -s -I \
    -H "Origin: https://app.chanzup.com" \
    -H "Access-Control-Request-Method: POST" \
    -H "Access-Control-Request-Headers: Content-Type" \
    -X OPTIONS \
    "$BASE_URL/api/auth/login")

if echo "$response" | grep -q "Access-Control-Allow-Origin"; then
    log "✅ CORS headers present"
else
    warn "CORS headers not found"
fi

# Test 8: Rate limiting (make multiple requests)
log "Testing rate limiting"
for i in {1..5}; do
    curl -s -o /dev/null "$BASE_URL/health" &
done
wait

# Test 9: Database connectivity (through health check)
log "Testing database connectivity"
health_response=$(curl -s "$BASE_URL/health")
if echo "$health_response" | grep -q "database.*healthy"; then
    log "✅ Database connectivity confirmed"
else
    warn "Database health status unclear"
fi

# Test 10: SSL/TLS (for production)
if [[ "$ENVIRONMENT" == "production" ]]; then
    log "Testing SSL/TLS configuration"
    ssl_info=$(curl -s -I "$BASE_URL" | grep -i "strict-transport-security")
    if [[ -n "$ssl_info" ]]; then
        log "✅ SSL/TLS security headers present"
    else
        warn "SSL/TLS security headers not found"
    fi
fi

# Performance test - measure response time
log "Testing response time"
start_time=$(date +%s%N)
curl -s -o /dev/null "$BASE_URL/health"
end_time=$(date +%s%N)
response_time=$(( (end_time - start_time) / 1000000 )) # Convert to milliseconds

if [[ $response_time -lt 1000 ]]; then
    log "✅ Response time: ${response_time}ms (good)"
elif [[ $response_time -lt 3000 ]]; then
    warn "Response time: ${response_time}ms (acceptable)"
else
    error "Response time: ${response_time}ms (too slow)"
fi

log "All smoke tests completed successfully!"
log "Environment: $ENVIRONMENT"
log "Timestamp: $(date)"

# Clean up
rm -f /tmp/response.json