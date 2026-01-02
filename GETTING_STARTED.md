# 🚀 Getting Started with Chanzup Local Development

Welcome to Chanzup! This guide will help you set up and run the complete application locally with all dependencies.

## 📋 What You'll Get

After following this guide, you'll have:
- ✅ Complete Chanzup application running locally
- ✅ Business back office portal with login functionality
- ✅ Client mobile app interface
- ✅ REST API with Swagger documentation
- ✅ Local database with demo data
- ✅ All dependencies properly configured

## 🎯 One-Command Setup

For the fastest setup experience:

```bash
# 1. Run the automated setup
./scripts/setup-local-env.sh

# 2. Verify everything is working
./scripts/verify-setup.sh

# 3. Start all services
./scripts/start-all.sh
```

That's it! Your application will be running at:
- **Back Office**: http://localhost:3000
- **Client App**: http://localhost:3001  
- **API**: https://localhost:7001

## 🔑 Demo Login

Use these credentials to test the business login:
- **Email**: `owner@coffeeshop.com`
- **Password**: `DemoPassword123!`

## 📋 Prerequisites

Before starting, ensure you have:

### Required
- **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** - For the API backend
- **[Node.js 18+](https://nodejs.org/)** - For React applications
- **SQL Server LocalDB** (Windows) or **Docker** (Mac/Linux) - For database

### Optional
- **[Docker Desktop](https://www.docker.com/products/docker-desktop)** - For containerized development
- **[Visual Studio Code](https://code.visualstudio.com/)** - Recommended IDE
- **[SQL Server Management Studio](https://docs.microsoft.com/en-us/sql/ssms/)** - For database management

## 🛠️ Manual Setup (If Needed)

If the automated setup doesn't work for your environment:

### 1. Install Dependencies

```bash
# .NET dependencies
cd src/Chanzup.API
dotnet restore
cd ../..

# Node.js dependencies
cd backoffice-app && npm install && cd ..
cd client-app && npm install && cd ..
```

### 2. Setup Database

#### Windows (LocalDB)
```bash
# Create LocalDB instance
sqllocaldb create MSSQLLocalDB -s

# Update database
cd src/Chanzup.API
dotnet ef database update --environment Local
cd ../..
```

#### Mac/Linux (Docker)
```bash
# Start SQL Server container
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=ChanzupLocal123!" \
  -p 1433:1433 --name chanzup-sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest

# Update connection string in src/Chanzup.API/appsettings.Local.json:
# "Server=localhost,1433;Database=ChanzupLocal;User Id=sa;Password=ChanzupLocal123!;TrustServerCertificate=true"
```

### 3. Setup HTTPS Certificates

```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

### 4. Seed Demo Data

```bash
cd src/Chanzup.API
dotnet run --environment Local --seed-demo
# Press Ctrl+C after seeing "Demo data seeding completed"
cd ../..
```

## 🏃‍♂️ Running the Application

### Option 1: All Services (Recommended)
```bash
./scripts/start-all.sh
```

### Option 2: Individual Services
```bash
# Terminal 1 - API Server
./scripts/start-api.sh

# Terminal 2 - Back Office App  
./scripts/start-backoffice.sh

# Terminal 3 - Client App
./scripts/start-client.sh
```

### Option 3: Docker Compose
```bash
docker-compose -f docker-compose.local.yml up --build
```

## 🌐 Application URLs

Once running, access these URLs:

| Service | URL | Description |
|---------|-----|-------------|
| Back Office | http://localhost:3000 | Business management portal |
| Client App | http://localhost:3001 | Customer mobile app |
| API Server | https://localhost:7001 | REST API backend |
| API Docs | https://localhost:7001/swagger | Interactive API documentation |
| Health Check | https://localhost:7001/health | System health status |

## 🧪 Testing Your Setup

### Quick Health Check
```bash
# Test API is running
curl -k https://localhost:7001/health

# Test business login
curl -k -X POST https://localhost:7001/api/auth/login/business \
  -H "Content-Type: application/json" \
  -d '{"email":"owner@coffeeshop.com","password":"DemoPassword123!"}'
```

### Comprehensive Tests
```bash
# Run all verification tests
./scripts/verify-setup.sh

# Run unit tests
cd src/Chanzup.Tests
dotnet test
```

## 🎮 Demo Data

The application includes comprehensive demo data:

### Demo Business
- **Name**: Demo Coffee Shop
- **Subscription**: Premium
- **Location**: Vancouver, BC

### Demo Staff Account
- **Email**: owner@coffeeshop.com
- **Password**: DemoPassword123!
- **Role**: Business Owner
- **Permissions**: Full access

### Demo Player Account  
- **Email**: demo@player.com
- **Password**: PlayerPassword123!
- **Tokens**: 25 free tokens

### Demo Campaign
- **Name**: Holiday Wheel of Fortune
- **Type**: Wheel of Luck
- **Prizes**: Free Coffee, 10% Discount, Free Pastry

## 🔧 Configuration

### Environment Variables (.env.local)
```bash
# Database
DATABASE_CONNECTION_STRING=Server=(localdb)\\MSSQLLocalDB;Database=ChanzupLocal;Trusted_Connection=true

# JWT Settings
JWT_SECRET_KEY=your-super-secret-key-that-is-at-least-32-characters-long-for-local-development

# API Configuration
REACT_APP_API_BASE_URL=https://localhost:7001
```

### API Settings (appsettings.Local.json)
```json
{
  "Jwt": {
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 30
  },
  "Security": {
    "RequireHttps": false,
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:3001"]
  }
}
```

## 🐛 Troubleshooting

### Common Issues

#### Port Already in Use
```bash
# Find and kill process using port 7001
lsof -ti:7001 | xargs kill -9

# Or use different ports in configuration
```

#### Certificate Issues
```bash
# Reset HTTPS certificates
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

#### Database Connection Issues
```bash
# Check LocalDB status (Windows)
sqllocaldb info MSSQLLocalDB
sqllocaldb start MSSQLLocalDB

# Or restart Docker container (Mac/Linux)
docker restart chanzup-sqlserver
```

#### Node Modules Issues
```bash
# Clear and reinstall dependencies
rm -rf backoffice-app/node_modules backoffice-app/package-lock.json
cd backoffice-app && npm install
```

### Debug Mode

Enable detailed logging by updating `appsettings.Local.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

## 📁 Project Structure

```
chanzup/
├── 🔧 Configuration
│   ├── .env.local                    # Main environment variables
│   ├── docker-compose.local.yml     # Docker configuration
│   └── src/Chanzup.API/appsettings.Local.json
├── 🏗️ Backend (.NET 8)
│   ├── src/Chanzup.API/             # REST API & Controllers
│   ├── src/Chanzup.Application/     # Business Logic & DTOs
│   ├── src/Chanzup.Domain/          # Domain Models & Entities
│   ├── src/Chanzup.Infrastructure/  # Data Access & Services
│   └── src/Chanzup.Tests/           # Unit & Integration Tests
├── 🎨 Frontend (React + TypeScript)
│   ├── backoffice-app/              # Business Management Portal
│   └── client-app/                  # Customer Mobile App
└── 🚀 Scripts
    ├── setup-local-env.sh           # Automated setup
    ├── verify-setup.sh              # Setup verification
    ├── start-all.sh                 # Start all services
    └── test-local-setup.sh          # Integration tests
```

## 🔄 Development Workflow

### 1. Make Changes
- Edit code in your preferred IDE
- React apps have hot reload enabled
- API requires restart for code changes

### 2. Test Changes
```bash
# Run unit tests
dotnet test

# Run integration tests  
./scripts/test-local-setup.sh

# Manual API testing
curl -k https://localhost:7001/api/health
```

### 3. Database Changes
```bash
# Add new migration
cd src/Chanzup.API
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update
```

### 4. Reset Demo Data
```bash
cd src/Chanzup.API
dotnet run --environment Local --seed-demo
```

## 🎯 What's Included

### ✅ Authentication & Authorization
- JWT-based authentication with refresh tokens
- Role-based access control (Owner, Manager, Staff)
- Permission-based authorization
- Multi-tenant business isolation
- Secure password hashing with BCrypt

### ✅ Business Management
- Business registration and login
- Dashboard with real-time metrics
- Campaign management
- Prize inventory tracking
- Analytics and reporting
- Staff management with location access

### ✅ Game Mechanics
- QR code scanning for location verification
- Wheel of fortune game engine
- Token-based economy
- Prize redemption system
- Anti-fraud protection

### ✅ Technical Features
- Clean Architecture with DDD patterns
- Entity Framework Core with SQL Server
- React with TypeScript and modern hooks
- Comprehensive error handling
- Security middleware and rate limiting
- Health checks and monitoring
- Docker support for easy deployment

## 📚 Next Steps

After successful setup:

1. **🎮 Explore the Applications**
   - Login to back office and explore the dashboard
   - Try the client app player experience
   - Test QR code scanning and games

2. **🔍 Review the Code**
   - Understand the Clean Architecture structure
   - Review authentication and authorization patterns
   - Explore the React components and state management

3. **🛠️ Start Development**
   - Create new features using existing patterns
   - Add new API endpoints and React components
   - Implement additional game types or business logic

4. **📖 Learn More**
   - Read [BUSINESS_LOGIN_IMPLEMENTATION.md](BUSINESS_LOGIN_IMPLEMENTATION.md) for detailed architecture
   - Check API documentation at https://localhost:7001/swagger
   - Explore the test suite for examples

## 🆘 Getting Help

If you encounter issues:

1. **Check Prerequisites**: Ensure all required software is installed
2. **Run Verification**: Use `./scripts/verify-setup.sh` to diagnose issues
3. **Check Logs**: Review terminal output for error messages
4. **Reset Environment**: Re-run `./scripts/setup-local-env.sh`
5. **Manual Setup**: Follow the manual setup steps if automation fails

## 🎉 Success!

You now have a fully functional Chanzup development environment! 

The application demonstrates modern full-stack development with:
- Secure authentication and authorization
- Real-time business analytics
- Interactive game mechanics
- Mobile-responsive design
- Production-ready architecture

Happy coding! 🚀