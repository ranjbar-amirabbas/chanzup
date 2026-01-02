#!/bin/bash

# Test script to verify Swagger documentation is working
set -e

echo "🧪 Testing Chanzup API Swagger Documentation..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${BLUE}[TEST]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[PASS]${NC} $1"
}

print_error() {
    echo -e "${RED}[FAIL]${NC} $1"
}

# Test Swagger JSON endpoint
test_swagger_json() {
    print_status "Testing Swagger JSON endpoint..."
    
    cd src/Chanzup.API
    
    # Start the application in background
    timeout 60s dotnet run --environment Local --no-build > /dev/null 2>&1 &
    API_PID=$!
    
    # Wait for application to start
    sleep 20
    
    # Test Swagger JSON endpoint
    if curl -k -s https://localhost:7001/swagger/v1/swagger.json | jq . > /dev/null 2>&1; then
        print_success "Swagger JSON endpoint is accessible and returns valid JSON"
        JSON_STATUS=0
    else
        print_error "Swagger JSON endpoint failed or returned invalid JSON"
        JSON_STATUS=1
    fi
    
    # Stop the application
    kill $API_PID 2>/dev/null || true
    wait $API_PID 2>/dev/null || true
    
    cd ../..
    return $JSON_STATUS
}

# Test Swagger UI endpoint
test_swagger_ui() {
    print_status "Testing Swagger UI endpoint..."
    
    cd src/Chanzup.API
    
    # Start the application in background
    timeout 60s dotnet run --environment Local --no-build > /dev/null 2>&1 &
    API_PID=$!
    
    # Wait for application to start
    sleep 20
    
    # Test Swagger UI endpoint
    UI_RESPONSE=$(curl -k -s -o /dev/null -w "%{http_code}" https://localhost:7001/swagger)
    
    if [ "$UI_RESPONSE" = "200" ]; then
        print_success "Swagger UI endpoint is accessible"
        UI_STATUS=0
    else
        print_error "Swagger UI endpoint returned status: $UI_RESPONSE"
        UI_STATUS=1
    fi
    
    # Stop the application
    kill $API_PID 2>/dev/null || true
    wait $API_PID 2>/dev/null || true
    
    cd ../..
    return $UI_STATUS
}

# Test authentication endpoints in Swagger
test_auth_endpoints() {
    print_status "Testing authentication endpoints..."
    
    cd src/Chanzup.API
    
    # Start the application in background
    timeout 60s dotnet run --environment Local --no-build > /dev/null 2>&1 &
    API_PID=$!
    
    # Wait for application to start
    sleep 20
    
    # Test business login endpoint
    LOGIN_RESPONSE=$(curl -k -s -X POST https://localhost:7001/api/auth/login/business \
        -H "Content-Type: application/json" \
        -d '{"email":"owner@coffeeshop.com","password":"DemoPassword123!"}' \
        -w "%{http_code}")
    
    if [[ $LOGIN_RESPONSE == *"200"* ]] && [[ $LOGIN_RESPONSE == *"accessToken"* ]]; then
        print_success "Authentication endpoints are working"
        AUTH_STATUS=0
    else
        print_error "Authentication endpoints failed"
        AUTH_STATUS=1
    fi
    
    # Stop the application
    kill $API_PID 2>/dev/null || true
    wait $API_PID 2>/dev/null || true
    
    cd ../..
    return $AUTH_STATUS
}

# Test health check endpoints
test_health_endpoints() {
    print_status "Testing health check endpoints..."
    
    cd src/Chanzup.API
    
    # Start the application in background
    timeout 60s dotnet run --environment Local --no-build > /dev/null 2>&1 &
    API_PID=$!
    
    # Wait for application to start
    sleep 20
    
    # Test health endpoint
    HEALTH_RESPONSE=$(curl -k -s https://localhost:7001/health -w "%{http_code}")
    
    if [[ $HEALTH_RESPONSE == *"200"* ]] && [[ $HEALTH_RESPONSE == *"Healthy"* ]]; then
        print_success "Health check endpoints are working"
        HEALTH_STATUS=0
    else
        print_error "Health check endpoints failed"
        HEALTH_STATUS=1
    fi
    
    # Stop the application
    kill $API_PID 2>/dev/null || true
    wait $API_PID 2>/dev/null || true
    
    cd ../..
    return $HEALTH_STATUS
}

# Validate Swagger configuration files
test_swagger_config() {
    print_status "Validating Swagger configuration files..."
    
    local errors=0
    
    # Check if configuration files exist
    if [ -f "src/Chanzup.API/Configuration/SwaggerConfiguration.cs" ]; then
        print_success "SwaggerConfiguration.cs exists"
    else
        print_error "SwaggerConfiguration.cs missing"
        errors=$((errors + 1))
    fi
    
    if [ -f "src/Chanzup.API/Configuration/SwaggerFilters.cs" ]; then
        print_success "SwaggerFilters.cs exists"
    else
        print_error "SwaggerFilters.cs missing"
        errors=$((errors + 1))
    fi
    
    if [ -f "src/Chanzup.API/wwwroot/swagger-ui/custom.css" ]; then
        print_success "Custom CSS file exists"
    else
        print_error "Custom CSS file missing"
        errors=$((errors + 1))
    fi
    
    # Check project file for XML documentation
    if grep -q "GenerateDocumentationFile" src/Chanzup.API/Chanzup.API.csproj; then
        print_success "XML documentation generation enabled"
    else
        print_error "XML documentation generation not enabled"
        errors=$((errors + 1))
    fi
    
    return $errors
}

# Main test function
main() {
    echo "╔══════════════════════════════════════════════════════════════╗"
    echo "║                    Swagger Documentation Test               ║"
    echo "╚══════════════════════════════════════════════════════════════╝"
    echo ""
    
    local total_tests=0
    local passed_tests=0
    
    # Run tests
    tests=(
        "test_swagger_config"
        "test_swagger_json"
        "test_swagger_ui"
        "test_auth_endpoints"
        "test_health_endpoints"
    )
    
    for test in "${tests[@]}"; do
        total_tests=$((total_tests + 1))
        if $test; then
            passed_tests=$((passed_tests + 1))
        fi
        echo ""
    done
    
    # Summary
    echo "╔══════════════════════════════════════════════════════════════╗"
    echo "║                      Test Results                           ║"
    echo "╚══════════════════════════════════════════════════════════════╝"
    echo ""
    
    if [ $passed_tests -eq $total_tests ]; then
        echo -e "${GREEN}✅ All Swagger tests passed! ($passed_tests/$total_tests)${NC}"
        echo ""
        echo "🎉 Swagger documentation is working perfectly!"
        echo ""
        echo "📖 Access Swagger UI at: https://localhost:7001/swagger"
        echo "🔧 API JSON at: https://localhost:7001/swagger/v1/swagger.json"
        echo ""
        echo "🔑 Demo Login Credentials:"
        echo "   Email: owner@coffeeshop.com"
        echo "   Password: DemoPassword123!"
        echo ""
        return 0
    else
        echo -e "${RED}❌ Some Swagger tests failed ($passed_tests/$total_tests)${NC}"
        echo ""
        echo "🔧 Please check the failed tests and ensure:"
        echo "   1. API is building successfully"
        echo "   2. All configuration files are present"
        echo "   3. Database is accessible"
        echo "   4. No port conflicts exist"
        echo ""
        return 1
    fi
}

# Run main function
main