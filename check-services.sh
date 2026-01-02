#!/bin/bash

echo "=== Chanzup Local Development Stack Status ==="
echo ""

echo "🔍 Checking service health..."
echo ""

# Check if containers are running
echo "📦 Container Status:"
docker-compose -f docker-compose.local.yml ps --format "table {{.Name}}\t{{.Status}}\t{{.Ports}}"
echo ""

# Check service endpoints
echo "🌐 Service Endpoints:"
echo "API Health: http://localhost:8080/health"
echo "Client App: http://localhost:3000"
echo "Backoffice: http://localhost:3001"
echo "Grafana: http://localhost:3002 (admin/admin)"
echo "Prometheus: http://localhost:9090"
echo "Mailhog: http://localhost:8025"
echo ""

# Test API health endpoint
echo "🏥 API Health Check:"
curl -s http://localhost:8080/health || echo "API not responding"
echo ""

echo "📊 Database Status:"
docker-compose -f docker-compose.local.yml exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P ChanzupDev123! -C -Q "SELECT 1" || echo "SQL Server not ready"
echo ""

echo "🔧 Redis Status:"
docker-compose -f docker-compose.local.yml exec -T redis redis-cli ping || echo "Redis not responding"
echo ""

echo "=== End Status Check ==="