# Chanzup Platform Deployment Guide

This document provides comprehensive instructions for deploying the Chanzup Platform to staging and production environments.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Environment Setup](#environment-setup)
3. [Database Setup](#database-setup)
4. [Deployment Methods](#deployment-methods)
5. [Monitoring and Health Checks](#monitoring-and-health-checks)
6. [Rollback Procedures](#rollback-procedures)
7. [Troubleshooting](#troubleshooting)

## Prerequisites

### Required Software
- Docker and Docker Compose
- .NET 9.0 SDK (for local development)
- Node.js 18+ (for frontend builds)
- Azure CLI (for Azure deployments)
- kubectl (for Kubernetes deployments)

### Required Services
- SQL Server database
- Redis cache
- Azure Storage Account
- Application Insights
- External API keys (Google Maps, Stripe, SendGrid)

### Environment Variables
Ensure all required environment variables are set in your deployment environment:

```bash
# Database
DATABASE_CONNECTION_STRING
REDIS_CONNECTION_STRING

# Authentication
JWT_SECRET_KEY
JWT_ISSUER
JWT_AUDIENCE

# External Services
GOOGLE_MAPS_API_KEY
STRIPE_PUBLISHABLE_KEY
STRIPE_SECRET_KEY
SENDGRID_API_KEY

# Azure Services
AZURE_STORAGE_CONNECTION_STRING
APPINSIGHTS_CONNECTION_STRING
```

## Environment Setup

### Staging Environment
```bash
# Set up staging environment
cp .env.staging.example .env.staging
# Edit .env.staging with your staging values

# Deploy to staging
./scripts/deploy.sh staging v1.0.0
```

### Production Environment
```bash
# Set up production environment
cp .env.production.example .env.production
# Edit .env.production with your production values

# Deploy to production
./scripts/deploy.sh production v1.0.0
```

## Database Setup

### Initial Database Setup
```bash
# Create and migrate database
./scripts/db-migrate.sh staging migrate

# Seed initial data
./scripts/db-migrate.sh staging seed
```

### Database Migrations
```bash
# Apply migrations
./scripts/db-migrate.sh production migrate

# Check migration status
./scripts/db-migrate.sh production status

# Rollback to specific migration
./scripts/db-migrate.sh production rollback InitialCreate
```

## Deployment Methods

### 1. Standard Deployment
Basic deployment using Docker Compose:

```bash
# Deploy to staging
./scripts/deploy.sh staging v1.0.0

# Deploy to production
./scripts/deploy.sh production v1.0.0
```

### 2. Blue-Green Deployment
Zero-downtime deployment using blue-green strategy:

```bash
# Blue-green deployment
./scripts/blue-green-deploy.sh production v1.0.0
```

### 3. CI/CD Pipeline
Automated deployment using GitHub Actions:

1. Push to `develop` branch triggers staging deployment
2. Push to `main` branch triggers production deployment
3. Manual approval required for production deployments

### 4. Manual Docker Deployment
```bash
# Build and tag image
docker build -t chanzup/platform-api:v1.0.0 .

# Push to registry
docker push chanzup/platform-api:v1.0.0

# Deploy using docker-compose
docker-compose -f docker-compose.production.yml up -d
```

## Monitoring and Health Checks

### Health Check Endpoints
- `/health` - Overall application health
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

### Monitoring Stack
- **Prometheus** - Metrics collection
- **Grafana** - Metrics visualization
- **Application Insights** - Application monitoring
- **Structured logging** - JSON logs with correlation IDs

### Key Metrics to Monitor
- API response times
- Error rates
- Database connection health
- Redis connectivity
- Active users and campaigns
- QR scan rates
- Wheel spin rates

### Alerts
Configure alerts for:
- High error rates (>5%)
- Slow response times (>2s)
- Database connectivity issues
- High authentication failure rates
- Low active user counts

## Rollback Procedures

### Application Rollback
```bash
# Rollback to previous version
./scripts/rollback.sh production v0.9.0

# Rollback with database migration
./scripts/rollback.sh production v0.9.0 --with-db-rollback
```

### Database Rollback
```bash
# Rollback database to specific migration
./scripts/db-migrate.sh production rollback MigrationName
```

### Emergency Rollback
In case of critical issues:

1. **Immediate rollback**:
   ```bash
   ./scripts/rollback.sh production
   ```

2. **Check application health**:
   ```bash
   curl -f https://api.chanzup.com/health
   ```

3. **Monitor logs**:
   ```bash
   docker-compose logs -f api
   ```

## Deployment Checklist

### Pre-Deployment
- [ ] All tests passing
- [ ] Security scan completed
- [ ] Database migrations tested
- [ ] Environment variables configured
- [ ] External services accessible
- [ ] Backup created

### During Deployment
- [ ] Health checks passing
- [ ] Smoke tests successful
- [ ] Metrics showing normal behavior
- [ ] No error spikes in logs

### Post-Deployment
- [ ] Application accessible
- [ ] Key user journeys working
- [ ] Database queries performing well
- [ ] External integrations working
- [ ] Monitoring alerts configured

## Troubleshooting

### Common Issues

#### Database Connection Issues
```bash
# Check database connectivity
./scripts/db-migrate.sh production status

# Test connection string
docker run --rm -e "ConnectionStrings__DefaultConnection=$DATABASE_CONNECTION_STRING" \
  chanzup/platform-api:latest dotnet ef database update --dry-run
```

#### Redis Connection Issues
```bash
# Test Redis connectivity
redis-cli -h prod-redis.chanzup.com -p 6379 ping
```

#### SSL Certificate Issues
```bash
# Check certificate expiration
openssl s_client -connect api.chanzup.com:443 -servername api.chanzup.com | openssl x509 -noout -dates
```

#### High Memory Usage
```bash
# Check container memory usage
docker stats

# Restart services if needed
docker-compose restart api
```

### Log Analysis
```bash
# View application logs
docker-compose logs -f api

# Search for errors
docker-compose logs api | grep -i error

# View structured logs
docker-compose logs api | jq '.timestamp, .level, .message'
```

### Performance Issues
```bash
# Check API response times
curl -w "@curl-format.txt" -o /dev/null -s https://api.chanzup.com/health

# Monitor database performance
# Use Application Insights or database monitoring tools
```

## Security Considerations

### SSL/TLS
- Use TLS 1.2 or higher
- Implement HSTS headers
- Regular certificate renewal

### Secrets Management
- Use Azure Key Vault for production secrets
- Rotate secrets regularly
- Never commit secrets to version control

### Network Security
- Implement proper firewall rules
- Use VPN for database access
- Enable DDoS protection

### Application Security
- Regular security scans
- Dependency updates
- Input validation
- Rate limiting

## Backup and Recovery

### Database Backups
```bash
# Create database backup
sqlcmd -S prod-db.chanzup.com -Q "BACKUP DATABASE ChanzupProd TO DISK = '/backups/chanzup-$(date +%Y%m%d).bak'"

# Restore from backup
sqlcmd -S prod-db.chanzup.com -Q "RESTORE DATABASE ChanzupProd FROM DISK = '/backups/chanzup-20240101.bak'"
```

### Application Data Backup
- Regular database backups
- Redis persistence enabled
- File storage backups (Azure Blob)
- Configuration backups

## Support and Escalation

### Contact Information
- **Development Team**: dev-team@chanzup.com
- **DevOps Team**: devops@chanzup.com
- **On-call Engineer**: +1-XXX-XXX-XXXX

### Escalation Procedures
1. **Level 1**: Development team (response: 1 hour)
2. **Level 2**: Senior engineers (response: 30 minutes)
3. **Level 3**: CTO/Technical leadership (response: 15 minutes)

### Documentation
- [API Documentation](https://api.chanzup.com/swagger)
- [Architecture Documentation](./docs/architecture.md)
- [Runbook](./docs/runbook.md)