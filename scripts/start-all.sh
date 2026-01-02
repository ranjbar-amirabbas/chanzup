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
