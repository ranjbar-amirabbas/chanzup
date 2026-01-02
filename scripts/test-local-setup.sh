#!/bin/bash

# Test script to verify local setup is working
set -e

echo "🧪 Testing Chanzup local setup..."

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

# Test .NET build
test_dotnet_build() {
    print_status "Testing .NET build..."
    
    cd src/Chanzup.API
    if dotnet build --configuration Debug --verbosity quiet; then
        print_success ".NET application builds successfully"
    else
        print_error ".NET build failed"
        return 1
    fi
    cd ../..
}

# Test Node.js dependencies
test_node_dependencies() {
    print_status "Testing Node.js dependencies..."
    
    # Test back office app
    cd backoffice-app
    if npm list --depth=0 > /dev/null 2>&1; then
        print_success "Back office app dependencies are installed"
    else
        print_error "Back office app dependencies missing"
        return 1
    fi
    cd ..
    
    # Test client app
    cd client-app
    if npm list --depth=0 > /dev/null 2>&1; then
        print_success "Client app dependencies are installed"
    else
        print_error "Client app dependencies missing"
        return 1
    fi
    cd ..
}

# Test database connection
test_database_connection() {
    print_status "Testing database connection..."
    
    cd src/Chanzup.API
    
    # Start the application in background
    timeout 30s dotnet run --environment Local --no-build > /dev/null 2>&1 &
    API_PID=$!
    
    # Wait for application to start
    sleep 15
    
    # Test health endpoint
    if curl -k -s https://localhost:7001/health > /dev/null 2>&1; then
        print_success "Database connection is working"
        HEALTH_STATUS=0
    else
        print_error "Database connection failed"
        HEALTH_STATUS=1
    fi
    
    # Stop the application
    kill $API_PID 2>/dev/null || true
    wait $API_PID 2>/dev/null || true
    
    cd ../..
    return $HEALTH_STATUS
}

# Test API endpoints
test_api_endpoints() {
    print_status "Testing API endpoints..."
    
    cd src/Chanzup.API
    
    # Start the application in background
    timeout 60s dotnet run --environment Local --no-build > /dev/null 2>&1 &
    API_PID=$!
    
    # Wait for application to start
    sleep 20
    
    # Test login endpoint
    LOGIN_RESPONSE=$(curl -k -s -X POST https://localhost:7001/api/auth/login/business \
        -H "Content-Type: application/json" \
        -d '{"email":"owner@coffeeshop.com","password":"DemoPassword123!"}' \
        -w "%{http_code}")
    
    if [[ $LOGIN_RESPONSE == *"200"* ]]; then
        print_success "Business login endpoint is working"
        API_STATUS=0
    else
        print_error "Business login endpoint failed"
        API_STATUS=1
    fi
    
    # Stop the application
    kill $API_PID 2>/dev/null || true
    wait $API_PID 2>/dev/null || true
    
    cd ../..
    return $API_STATUS
}

# Test React app builds
test_react_builds() {
    print_status "Testing React app builds..."
    
    # Test back office app build
    cd backoffice-app
    if timeout 60s npm run build > /dev/null 2>&1; then
        print_success "Back office app builds successfully"
        BACKOFFICE_STATUS=0
    else
        print_error "Back office app build failed"
        BACKOFFICE_STATUS=1
    fi
    cd ..
    
    # Test client app build
    cd client-app
    if timeout 60s npm run build > /dev/null 2>&1; then
        print_success "Client app builds successfully"
        CLIENT_STATUS=0
    else
        print_error "Client app build failed"
        CLIENT_STATUS=1
    fi
    cd ..
    
    return $((BACKOFFICE_STATUS + CLIENT_STATUS))
}

# Main test function
main() {
    echo "╔══════════════════════════════════════════════════════════════╗"
    echo "║                    Chanzup Setup Test                       ║"
    echo "╚══════════════════════════════════════════════════════════════╝"
    echo ""
    
    TOTAL_TESTS=0
    PASSED_TESTS=0
    
    # Run tests
    tests=(
        "test_dotnet_build"
        "test_node_dependencies"
        "test_database_connection"
        "test_api_endpoints"
        "test_react_builds"
    )
    
    for test in "${tests[@]}"; do
        TOTAL_TESTS=$((TOTAL_TESTS + 1))
        if $test; then
            PASSED_TESTS=$((PASSED_TESTS + 1))
        fi
        echo ""
    done
    
    # Summary
    echo "╔══════════════════════════════════════════════════════════════╗"
    echo "║                      Test Results                           ║"
    echo "╚══════════════════════════════════════════════════════════════╝"
    echo ""
    
    if [ $PASSED_TESTS -eq $TOTAL_TESTS ]; then
        echo -e "${GREEN}✅ All tests passed! ($PASSED_TESTS/$TOTAL_TESTS)${NC}"
        echo ""
        echo "🚀 Your local environment is ready!"
        echo "   Run: ./scripts/start-all.sh"
        echo ""
        return 0
    else
        echo -e "${RED}❌ Some tests failed ($PASSED_TESTS/$TOTAL_TESTS)${NC}"
        echo ""
        echo "🔧 Please check the failed tests and run setup again:"
        echo "   ./scripts/setup-local-env.sh"
        echo ""
        return 1
    fi
}

# Run main function
main