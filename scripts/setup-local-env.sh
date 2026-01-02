#!/bin/bash

# Chanzup Local Environment Setup Script
set -e

echo "🚀 Setting up Chanzup local development environment..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check prerequisites
check_prerequisites() {
    print_status "Checking prerequisites..."
    
    # Check if .NET 8 SDK is installed
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET 8 SDK is not installed. Please install it from https://dotnet.microsoft.com/download"
        exit 1
    fi
    
    # Check .NET version
    DOTNET_VERSION=$(dotnet --version)
    print_success ".NET SDK version: $DOTNET_VERSION"
    
    # Check if Node.js is installed
    if ! command -v node &> /dev/null; then
        print_error "Node.js is not installed. Please install it from https://nodejs.org/"
        exit 1
    fi
    
    # Check Node.js version
    NODE_VERSION=$(node --version)
    print_success "Node.js version: $NODE_VERSION"
    
    # Check if npm is installed
    if ! command -v npm &> /dev/null; then
        print_error "npm is not installed. Please install Node.js which includes npm."
        exit 1
    fi
    
    # Check npm version
    NPM_VERSION=$(npm --version)
    print_success "npm version: $NPM_VERSION"
    
    # Check if Docker is installed (optional)
    if command -v docker &> /dev/null; then
        DOCKER_VERSION=$(docker --version)
        print_success "Docker version: $DOCKER_VERSION"
        DOCKER_AVAILABLE=true
    else
        print_warning "Docker is not installed. You can still run the application without Docker."
        DOCKER_AVAILABLE=false
    fi
}

# Setup .NET development certificates
setup_dev_certificates() {
    print_status "Setting up .NET development certificates..."
    
    # Clean existing certificates
    dotnet dev-certs https --clean
    
    # Generate new certificate
    dotnet dev-certs https --trust
    
    print_success "Development certificates configured"
}

# Setup database
setup_database() {
    print_status "Setting up local database..."
    
    # Check if LocalDB is available (Windows)
    if command -v sqllocaldb &> /dev/null; then
        print_status "Using SQL Server LocalDB..."
        
        # Create LocalDB instance if it doesn't exist
        sqllocaldb create MSSQLLocalDB -s
        
        print_success "LocalDB configured"
    else
        print_warning "LocalDB not available. You may need to use Docker for SQL Server or update connection string."
    fi
}

# Install .NET dependencies
install_dotnet_dependencies() {
    print_status "Installing .NET dependencies..."
    
    cd src/Chanzup.API
    dotnet restore
    cd ../..
    
    print_success ".NET dependencies installed"
}

# Install Node.js dependencies
install_node_dependencies() {
    print_status "Installing Node.js dependencies for back office app..."
    
    cd backoffice-app
    npm install
    cd ..
    
    print_status "Installing Node.js dependencies for client app..."
    
    cd client-app
    npm install
    cd ..
    
    print_success "Node.js dependencies installed"
}

# Build the application
build_application() {
    print_status "Building .NET application..."
    
    cd src/Chanzup.API
    dotnet build --configuration Debug
    cd ../..
    
    print_success ".NET application built successfully"
}

# Setup database and seed data
setup_database_and_seed() {
    print_status "Setting up database schema and seeding demo data..."
    
    cd src/Chanzup.API
    
    # Run database migrations
    ASPNETCORE_ENVIRONMENT=Local dotnet ef database update --configuration Debug
    
    # Seed demo data
    dotnet run --environment Local --seed-demo &
    API_PID=$!
    
    # Wait a moment for the application to start
    sleep 10
    
    # Stop the application
    kill $API_PID 2>/dev/null || true
    
    cd ../..
    
    print_success "Database setup and seeding completed"
}

# Create launch scripts
create_launch_scripts() {
    print_status "Creating launch scripts..."
    
    # API launch script
    cat > scripts/start-api.sh << 'EOF'
#!/bin/bash
echo "🚀 Starting Chanzup API..."
cd src/Chanzup.API
dotnet run --environment Local
EOF
    
    # Back office app launch script
    cat > scripts/start-backoffice.sh << 'EOF'
#!/bin/bash
echo "🚀 Starting Chanzup Back Office App..."
cd backoffice-app
npm start
EOF
    
    # Client app launch script
    cat > scripts/start-client.sh << 'EOF'
#!/bin/bash
echo "🚀 Starting Chanzup Client App..."
cd client-app
npm start
EOF
    
    # All services launch script
    cat > scripts/start-all.sh << 'EOF'
#!/bin/bash
echo "🚀 Starting all Chanzup services..."

# Function to cleanup background processes
cleanup() {
    echo "Stopping all services..."
    jobs -p | xargs -r kill
    exit 0
}

# Set trap to cleanup on script exit
trap cleanup SIGINT SIGTERM

# Start API in background
echo "Starting API..."
cd src/Chanzup.API
dotnet run --environment Local &
API_PID=$!
cd ../..

# Wait for API to start
sleep 10

# Start Back Office App in background
echo "Starting Back Office App..."
cd backoffice-app
npm start &
BACKOFFICE_PID=$!
cd ..

# Start Client App in background
echo "Starting Client App..."
cd client-app
npm start &
CLIENT_PID=$!
cd ..

echo ""
echo "✅ All services started!"
echo "📊 Back Office: http://localhost:3000"
echo "📱 Client App: http://localhost:3001"
echo "🔧 API: https://localhost:7001"
echo "📖 API Docs: https://localhost:7001/swagger"
echo "❤️ Health Check: https://localhost:7001/health"
echo ""
echo "Demo Login Credentials:"
echo "Email: owner@coffeeshop.com"
echo "Password: DemoPassword123!"
echo ""
echo "Press Ctrl+C to stop all services"

# Wait for all background processes
wait
EOF
    
    # Make scripts executable
    chmod +x scripts/start-api.sh
    chmod +x scripts/start-backoffice.sh
    chmod +x scripts/start-client.sh
    chmod +x scripts/start-all.sh
    
    print_success "Launch scripts created"
}

# Docker setup (optional)
setup_docker() {
    if [ "$DOCKER_AVAILABLE" = true ]; then
        print_status "Setting up Docker environment..."
        
        # Create Docker launch script
        cat > scripts/start-docker.sh << 'EOF'
#!/bin/bash
echo "🐳 Starting Chanzup with Docker..."

# Build and start all services
docker-compose -f docker-compose.local.yml up --build

echo "✅ Docker services started!"
echo "📊 Back Office: http://localhost:3000"
echo "📱 Client App: http://localhost:3001"
echo "🔧 API: https://localhost:7001"
echo "🗄️ SQL Server: localhost:1433"
echo "🔴 Redis: localhost:6379"
EOF
        
        chmod +x scripts/start-docker.sh
        
        print_success "Docker setup completed"
    fi
}

# Main setup function
main() {
    echo "╔══════════════════════════════════════════════════════════════╗"
    echo "║                    Chanzup Local Setup                      ║"
    echo "╚══════════════════════════════════════════════════════════════╝"
    echo ""
    
    check_prerequisites
    setup_dev_certificates
    setup_database
    install_dotnet_dependencies
    install_node_dependencies
    build_application
    setup_database_and_seed
    create_launch_scripts
    setup_docker
    
    echo ""
    echo "╔══════════════════════════════════════════════════════════════╗"
    echo "║                    Setup Complete! 🎉                       ║"
    echo "╚══════════════════════════════════════════════════════════════╝"
    echo ""
    echo "🚀 Quick Start Options:"
    echo ""
    echo "1️⃣  Start all services:"
    echo "   ./scripts/start-all.sh"
    echo ""
    echo "2️⃣  Start services individually:"
    echo "   ./scripts/start-api.sh          # API Server"
    echo "   ./scripts/start-backoffice.sh   # Back Office App"
    echo "   ./scripts/start-client.sh       # Client App"
    echo ""
    if [ "$DOCKER_AVAILABLE" = true ]; then
        echo "3️⃣  Start with Docker:"
        echo "   ./scripts/start-docker.sh"
        echo ""
    fi
    echo "📊 Back Office Portal: http://localhost:3000/login"
    echo "📱 Client App: http://localhost:3001"
    echo "🔧 API: https://localhost:7001"
    echo "📖 API Documentation: https://localhost:7001/swagger"
    echo ""
    echo "🔑 Demo Login Credentials:"
    echo "   Email: owner@coffeeshop.com"
    echo "   Password: DemoPassword123!"
    echo ""
    echo "📚 For more information, see BUSINESS_LOGIN_IMPLEMENTATION.md"
}

# Run main function
main