#!/bin/bash

# Comprehensive setup verification script
set -e

echo "🔍 Verifying Chanzup local environment setup..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_header() {
    echo ""
    echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
    echo -e "${BLUE} $1${NC}"
    echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
}

print_status() {
    echo -e "${BLUE}[CHECK]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[✓]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[!]${NC} $1"
}

print_error() {
    echo -e "${RED}[✗]${NC} $1"
}

# Check if file exists
check_file() {
    if [ -f "$1" ]; then
        print_success "File exists: $1"
        return 0
    else
        print_error "File missing: $1"
        return 1
    fi
}

# Check if directory exists
check_directory() {
    if [ -d "$1" ]; then
        print_success "Directory exists: $1"
        return 0
    else
        print_error "Directory missing: $1"
        return 1
    fi
}

# Verify prerequisites
verify_prerequisites() {
    print_header "Prerequisites Check"
    
    local errors=0
    
    # Check .NET SDK
    if command -v dotnet &> /dev/null; then
        local dotnet_version=$(dotnet --version)
        print_success ".NET SDK installed: $dotnet_version"
    else
        print_error ".NET SDK not found"
        errors=$((errors + 1))
    fi
    
    # Check Node.js
    if command -v node &> /dev/null; then
        local node_version=$(node --version)
        print_success "Node.js installed: $node_version"
    else
        print_error "Node.js not found"
        errors=$((errors + 1))
    fi
    
    # Check npm
    if command -v npm &> /dev/null; then
        local npm_version=$(npm --version)
        print_success "npm installed: $npm_version"
    else
        print_error "npm not found"
        errors=$((errors + 1))
    fi
    
    # Check Docker (optional)
    if command -v docker &> /dev/null; then
        local docker_version=$(docker --version)
        print_success "Docker installed: $docker_version"
    else
        print_warning "Docker not installed (optional)"
    fi
    
    return $errors
}

# Verify project structure
verify_project_structure() {
    print_header "Project Structure Check"
    
    local errors=0
    
    # Core directories
    local directories=(
        "src"
        "src/Chanzup.API"
        "src/Chanzup.Application"
        "src/Chanzup.Domain"
        "src/Chanzup.Infrastructure"
        "src/Chanzup.Tests"
        "backoffice-app"
        "client-app"
        "scripts"
    )
    
    for dir in "${directories[@]}"; do
        if ! check_directory "$dir"; then
            errors=$((errors + 1))
        fi
    done
    
    # Core files
    local files=(
        "src/Chanzup.API/Program.cs"
        "src/Chanzup.API/Chanzup.API.csproj"
        "backoffice-app/package.json"
        "client-app/package.json"
        ".env.local"
        "src/Chanzup.API/appsettings.Local.json"
        "docker-compose.local.yml"
    )
    
    for file in "${files[@]}"; do
        if ! check_file "$file"; then
            errors=$((errors + 1))
        fi
    done
    
    return $errors
}

# Verify configuration files
verify_configuration() {
    print_header "Configuration Check"
    
    local errors=0
    
    # Check .env.local
    if [ -f ".env.local" ]; then
        print_success "Main environment file exists"
        
        # Check for required variables
        local required_vars=(
            "DATABASE_CONNECTION_STRING"
            "JWT_SECRET_KEY"
            "REACT_APP_API_BASE_URL"
        )
        
        for var in "${required_vars[@]}"; do
            if grep -q "^$var=" .env.local; then
                print_success "Environment variable set: $var"
            else
                print_error "Environment variable missing: $var"
                errors=$((errors + 1))
            fi
        done
    else
        print_error "Main environment file missing: .env.local"
        errors=$((errors + 1))
    fi
    
    # Check API configuration
    if [ -f "src/Chanzup.API/appsettings.Local.json" ]; then
        print_success "API configuration file exists"
        
        # Validate JSON
        if python3 -m json.tool src/Chanzup.API/appsettings.Local.json > /dev/null 2>&1; then
            print_success "API configuration is valid JSON"
        else
            print_error "API configuration has invalid JSON"
            errors=$((errors + 1))
        fi
    else
        print_error "API configuration missing: appsettings.Local.json"
        errors=$((errors + 1))
    fi
    
    return $errors
}

# Verify dependencies
verify_dependencies() {
    print_header "Dependencies Check"
    
    local errors=0
    
    # Check .NET dependencies
    print_status "Checking .NET dependencies..."
    cd src/Chanzup.API
    if dotnet restore --verbosity quiet; then
        print_success ".NET dependencies restored"
    else
        print_error ".NET dependency restoration failed"
        errors=$((errors + 1))
    fi
    cd ../..
    
    # Check back office dependencies
    print_status "Checking back office app dependencies..."
    cd backoffice-app
    if [ -d "node_modules" ]; then
        print_success "Back office dependencies installed"
    else
        print_warning "Back office dependencies not installed, installing..."
        if npm install --silent; then
            print_success "Back office dependencies installed successfully"
        else
            print_error "Back office dependency installation failed"
            errors=$((errors + 1))
        fi
    fi
    cd ..
    
    # Check client app dependencies
    print_status "Checking client app dependencies..."
    cd client-app
    if [ -d "node_modules" ]; then
        print_success "Client app dependencies installed"
    else
        print_warning "Client app dependencies not installed, installing..."
        if npm install --silent; then
            print_success "Client app dependencies installed successfully"
        else
            print_error "Client app dependency installation failed"
            errors=$((errors + 1))
        fi
    fi
    cd ..
    
    return $errors
}

# Verify build process
verify_builds() {
    print_header "Build Verification"
    
    local errors=0
    
    # Test .NET build
    print_status "Testing .NET build..."
    cd src/Chanzup.API
    if dotnet build --configuration Debug --verbosity quiet; then
        print_success ".NET application builds successfully"
    else
        print_error ".NET build failed"
        errors=$((errors + 1))
    fi
    cd ../..
    
    # Test React builds (quick check)
    print_status "Testing React TypeScript compilation..."
    cd backoffice-app
    if npx tsc --noEmit --skipLibCheck; then
        print_success "Back office TypeScript compiles successfully"
    else
        print_error "Back office TypeScript compilation failed"
        errors=$((errors + 1))
    fi
    cd ..
    
    cd client-app
    if npx tsc --noEmit --skipLibCheck; then
        print_success "Client app TypeScript compiles successfully"
    else
        print_error "Client app TypeScript compilation failed"
        errors=$((errors + 1))
    fi
    cd ..
    
    return $errors
}

# Verify scripts
verify_scripts() {
    print_header "Scripts Check"
    
    local errors=0
    
    local scripts=(
        "scripts/setup-local-env.sh"
        "scripts/start-all.sh"
        "scripts/start-api.sh"
        "scripts/start-backoffice.sh"
        "scripts/start-client.sh"
        "scripts/test-local-setup.sh"
    )
    
    for script in "${scripts[@]}"; do
        if [ -f "$script" ]; then
            if [ -x "$script" ]; then
                print_success "Script exists and is executable: $script"
            else
                print_warning "Script exists but not executable: $script"
                chmod +x "$script"
                print_success "Made script executable: $script"
            fi
        else
            print_error "Script missing: $script"
            errors=$((errors + 1))
        fi
    done
    
    return $errors
}

# Generate setup report
generate_report() {
    local total_errors=$1
    
    print_header "Setup Report"
    
    if [ $total_errors -eq 0 ]; then
        echo -e "${GREEN}"
        echo "🎉 SETUP VERIFICATION COMPLETE!"
        echo ""
        echo "✅ All checks passed successfully"
        echo "✅ Your local environment is ready to run"
        echo ""
        echo "🚀 Quick Start Commands:"
        echo "   ./scripts/start-all.sh     # Start all services"
        echo "   ./scripts/test-local-setup.sh  # Run integration tests"
        echo ""
        echo "🌐 Access Points (after starting):"
        echo "   Back Office: http://localhost:3000"
        echo "   Client App:  http://localhost:3001"
        echo "   API Server:  https://localhost:7001"
        echo "   API Docs:    https://localhost:7001/swagger"
        echo ""
        echo "🔑 Demo Credentials:"
        echo "   Email:    owner@coffeeshop.com"
        echo "   Password: DemoPassword123!"
        echo -e "${NC}"
        return 0
    else
        echo -e "${RED}"
        echo "❌ SETUP VERIFICATION FAILED"
        echo ""
        echo "Found $total_errors error(s) that need to be resolved"
        echo ""
        echo "🔧 Recommended Actions:"
        echo "1. Review the errors above"
        echo "2. Run the setup script: ./scripts/setup-local-env.sh"
        echo "3. Install missing prerequisites"
        echo "4. Run this verification again"
        echo -e "${NC}"
        return 1
    fi
}

# Main verification function
main() {
    echo "╔══════════════════════════════════════════════════════════════╗"
    echo "║                 Chanzup Setup Verification                  ║"
    echo "╚══════════════════════════════════════════════════════════════╝"
    
    local total_errors=0
    
    # Run all verification steps
    verify_prerequisites
    total_errors=$((total_errors + $?))
    
    verify_project_structure
    total_errors=$((total_errors + $?))
    
    verify_configuration
    total_errors=$((total_errors + $?))
    
    verify_dependencies
    total_errors=$((total_errors + $?))
    
    verify_builds
    total_errors=$((total_errors + $?))
    
    verify_scripts
    total_errors=$((total_errors + $?))
    
    # Generate final report
    generate_report $total_errors
    
    return $total_errors
}

# Run main function
main