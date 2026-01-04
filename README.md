# 🎮 Chanzup - Gamification Platform

A comprehensive gamification platform that enables businesses to create engaging QR code-based campaigns and manage customer interactions through interactive games like Wheel of Fortune.

## 🚀 Features

### 🏢 Business Management
- **Multi-tenant Architecture** - Secure business isolation
- **Role-based Access Control** - Owner, Manager, Staff permissions
- **Real-time Dashboard** - Campaign metrics and analytics
- **Subscription Management** - Basic, Premium, Enterprise tiers

### 🎰 Game Mechanics
- **Wheel of Fortune** - Interactive spinning wheel games
- **QR Code Integration** - Location-based game activation
- **Prize Management** - Configurable rewards and inventory
- **Anti-fraud Protection** - Duplicate prevention and validation

### 🔐 Authentication & Security
- **JWT Authentication** - Secure token-based auth
- **Refresh Token System** - Automatic token renewal
- **Multi-factor Security** - Rate limiting and IP protection
- **Audit Logging** - Comprehensive security tracking

### 📊 Analytics & Reporting
- **Campaign Performance** - Engagement and conversion metrics
- **Player Analytics** - Customer behavior insights
- **Revenue Tracking** - Token sales and redemption data
- **Export Capabilities** - Data export for analysis

## 🏗️ Architecture

### Backend (.NET 8)
- **Clean Architecture** - Domain-driven design patterns
- **Entity Framework Core** - Database ORM with SQL Server
- **ASP.NET Core API** - RESTful API with Swagger documentation
- **Background Services** - Automated tasks and cleanup

### Frontend (React + TypeScript)
- **Back Office Portal** - Business management interface
- **Client Mobile App** - Customer-facing game interface
- **Responsive Design** - Mobile-first approach
- **Real-time Updates** - Live dashboard metrics

### Infrastructure
- **Docker Support** - Containerized deployment
- **Health Checks** - System monitoring and alerts
- **Caching Layer** - Redis for performance optimization
- **File Storage** - Azure Blob Storage integration

## 📋 Prerequisites

- **.NET 8 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js 18+** - [Download here](https://nodejs.org/)
- **SQL Server** - LocalDB (Windows) or Docker (Mac/Linux)
- **Docker** (Optional) - For containerized development

## 🚀 Quick Start

### Automated Setup
```bash
# Clone the repository
git clone https://gitlab.com/amirabbas-ranjbar/chanzup.git
cd chanzup

# Run automated setup
./scripts/setup-local-env.sh

# Verify setup
./scripts/verify-setup.sh

# Start all services
./scripts/start-all.sh
```

### Manual Setup
```bash
# Install .NET dependencies
cd src/Chanzup.API && dotnet restore && cd ../..

# Install Node.js dependencies
cd backoffice-app && npm install && cd ..
cd client-app && npm install && cd ..

# Setup database
cd src/Chanzup.API
dotnet ef database update --environment Local
dotnet run --environment Local --seed-demo
cd ../..

# Start services individually
./scripts/start-api.sh          # Terminal 1
./scripts/start-backoffice.sh   # Terminal 2
./scripts/start-client.sh       # Terminal 3
```

## 🌐 Access Points

| Service | URL | Description |
|---------|-----|-------------|
| **Back Office** | http://localhost:3000 | Business management portal |
| **Client App** | http://localhost:3001 | Customer mobile interface |
| **API Server** | https://localhost:7001 | REST API backend |
| **API Docs** | https://localhost:7001/swagger | Interactive API documentation |
| **Health Check** | https://localhost:7001/health | System status monitoring |

## 🔑 Demo Credentials

### Business Login (Back Office)
- **Email**: `owner@coffeeshop.com`
- **Password**: `DemoPassword123!`
- **Role**: Business Owner (Full Access)

### Player Login (Client App)
- **Email**: `demo@player.com`
- **Password**: `PlayerPassword123!`
- **Tokens**: 25 free tokens

## 📖 API Documentation

Comprehensive Swagger documentation is available at https://localhost:7001/swagger

### Key Endpoints

#### 🔐 Authentication
- `POST /api/auth/login/business` - Business staff login
- `POST /api/auth/login/player` - Player authentication
- `POST /api/auth/refresh` - Token refresh
- `POST /api/auth/register/business` - Business registration

#### 🏢 Business Management
- `GET /api/business/info` - Business details
- `GET /api/business/dashboard/metrics` - Dashboard analytics

#### 🎰 Game Mechanics
- `POST /api/qr/scan` - QR code scanning
- `POST /api/wheel/spin` - Wheel of fortune game
- `GET /api/campaigns` - Campaign management

#### ❤️ Health Monitoring
- `GET /health` - Comprehensive health check
- `GET /health/ready` - Readiness probe
- `GET /health/live` - Liveness probe

## 🧪 Testing

### Unit Tests
```bash
cd src/Chanzup.Tests
dotnet test
```

### Integration Tests
```bash
# Test complete setup
./scripts/test-local-setup.sh

# Test Swagger documentation
./scripts/test-swagger.sh

# Test API endpoints
curl -k https://localhost:7001/health
```

### Manual Testing
1. **Business Portal**: Login and explore dashboard
2. **Campaign Creation**: Set up a new wheel campaign
3. **QR Code Generation**: Create location-based codes
4. **Game Testing**: Scan QR and play wheel game
5. **Prize Redemption**: Redeem won prizes

## 🗄️ Database Schema

### Core Entities
- **Business** - Company accounts with subscription tiers
- **Staff** - Business employees with role-based access
- **Player** - End customers with token balances
- **Campaign** - Marketing campaigns with game configuration
- **Prize** - Rewards with inventory and probability settings
- **QRSession** - Location-based game sessions
- **WheelSpin** - Game results and prize distribution

### Key Relationships
- Business → Staff (1:Many)
- Business → Campaign (1:Many)
- Campaign → Prize (1:Many)
- Player → QRSession (1:Many)
- QRSession → WheelSpin (1:Many)

## 🔧 Configuration

### Environment Variables
```bash
# Database
DATABASE_CONNECTION_STRING=Server=(localdb)\\MSSQLLocalDB;Database=ChanzupLocal;Trusted_Connection=true

# JWT Settings
JWT_SECRET_KEY=your-super-secret-key-that-is-at-least-32-characters-long
JWT_ACCESS_TOKEN_EXPIRATION_MINUTES=60

# API Configuration
REACT_APP_API_BASE_URL=https://localhost:7001
```

### Subscription Tiers
- **Basic** (Free) - 1 campaign, basic analytics
- **Premium** ($29/month) - 5 campaigns, advanced analytics
- **Enterprise** ($99/month) - Unlimited campaigns, custom features

## 🚢 Deployment

### Docker Compose
```bash
# Production deployment
docker-compose -f docker-compose.production.yml up -d

# Local development
docker-compose -f docker-compose.local.yml up --build
```

### Environment-specific Configurations
- **Development** - Local database, detailed logging
- **Staging** - Staging database, moderate logging
- **Production** - Production database, minimal logging

## 📚 Documentation

- **[Getting Started Guide](GETTING_STARTED.md)** - Complete setup instructions
- **[Business Login Implementation](BUSINESS_LOGIN_IMPLEMENTATION.md)** - Authentication architecture
- **[Swagger Setup](SWAGGER_SETUP.md)** - API documentation guide
- **[Local Development](README.local.md)** - Development environment setup

## 🤝 Contributing

1. **Fork the repository**
2. **Create feature branch**: `git checkout -b feature/amazing-feature`
3. **Commit changes**: `git commit -m 'Add amazing feature'`
4. **Push to branch**: `git push origin feature/amazing-feature`
5. **Open Pull Request**

### Development Guidelines
- Follow Clean Architecture patterns
- Write comprehensive tests
- Update documentation
- Use conventional commit messages
- Ensure code coverage > 80%

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

### Getting Help
- **Issues**: [GitHub Issues](https://github.com/ranjbar-amirabbas/chanzup/issues)
- **Documentation**: Available in `/docs` folder
- **API Reference**: https://localhost:7001/swagger

### Troubleshooting
- **Setup Issues**: Run `./scripts/verify-setup.sh`
- **Port Conflicts**: Check `lsof -ti:7001` and kill processes
- **Database Issues**: Restart LocalDB or Docker containers
- **Certificate Issues**: Run `dotnet dev-certs https --trust`

## 🎯 Roadmap

### Version 2.0
- [ ] Mobile native apps (iOS/Android)
- [ ] Advanced game types (Scratch cards, Slot machines)
- [ ] Social sharing and referrals
- [ ] Advanced analytics and ML insights

### Version 3.0
- [ ] Multi-language support
- [ ] White-label solutions
- [ ] Advanced fraud detection
- [ ] Enterprise SSO integration

## 🏆 Acknowledgments

- **ASP.NET Core Team** - Excellent web framework
- **React Team** - Amazing frontend library
- **Entity Framework Team** - Powerful ORM
- **Swagger/OpenAPI** - API documentation standard

---

**Built with ❤️ by the Chanzup Team**

*Transforming customer engagement through gamification* 🎮