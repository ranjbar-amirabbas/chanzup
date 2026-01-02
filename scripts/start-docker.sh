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
