# Chanzup Local Development Setup

This guide will help you set up and run the complete Chanzup application locally with all dependencies.

## 🚀 Quick Start

### Option 1: Automated Setup (Recommended)
```bash
# Run the automated setup script
./scripts/setup-local-env.sh

# Start all services
./scripts/start-all.sh
```

### Option 2: Manual Setup
Follow the detailed steps below if you prefer manual setup or encounter issues with the automated script.

## 📋 Prerequisites

### Required Software
- **.NET 8 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js 18+** - [Download here](https://nodejs.org/)
- **SQL Server LocalDB** (Windows) or **Docker** (Mac/Linux)

### Optional Software
- **Docker Desktop** - For containerized development
- **Visual Studio 2022** or **VS Code** - For development
- **SQL Server Management Studio** - For database management

## 🛠️ Manual Setup Steps

### 1. Clone and Navigate
```bash
git clone <repository-url>
cd chanzup
```

### 2. Setup .NET Development Certificates
```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

### 3. Install Dependencies

#### .NET Dependencies
```bash
cd src/Chanzup.API
dotnet restore
cd ../..
```

#### Node.js Dependencies
```bash
# Back Office App
cd backoffice-app
npm install
cd ..

# Client App
cd client-app
npm install
cd ..
```

### 4. Database Setup

#### Option A: SQL Server LocalDB (Windows)
```bash
# Create LocalDB instance
sqllocaldb create MSSQLLocalDB -s

# Update database schema
cd src/Chanzup.API
dotnet ef database update --environment Local
cd ../..
```

#### Option B: Docker SQL Server (Mac/Linux)
```bash
# Start SQL Server container
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=ChanzupLocal123!" \
  -p 1433:1433 --name chanzup-sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest

# Update connection string in appsettings.Local.json
# "Server=localhost,1433;Database=ChanzupLocal;User Id=sa;Password=ChanzupLocal123!;TrustServerCertificate=true"
```

### 5. Build Application
```bash
cd src/Chanzup.API
dotnet build --configuration Debug
cd ../..
```

### 6. Seed Demo Data
```bash
cd src/Chanzup.API
dotnet run --environment Local --seed-demo
# Press Ctrl+C after seeing "Demo data seeding completed"
cd ../..
```

## 🏃‍♂️ Running the Application

### Option 1: All Services at Once
```bash
./scripts/start-all.sh
```

### Option 2: Individual Services

#### Start API Server
```bash
cd src/Chanzup.API
dotnet run --environment Local
```

#### Start Back Office App (New Terminal)
```bash
cd backoffice-app
npm start
```

#### Start Client App (New Terminal)
```bash
cd client-app
npm start
```

### Option 3: Docker Compose
```bash
docker-compose -f docker-compose.local.yml up --build
```

## 🌐 Access Points

Once all services are running:

- **Back Office Portal**: http://localhost:3000
- **Client App**: http://localhost:3001
- **API Server**: https://localhost:7001
- **API Documentation**: https://localhost:7001/swagger

## 🔑 Demo Credentials

### Business Login (Back Office)
- **Email**: `owner@coffeeshop.com`
- **Password**: `DemoPassword123!`

### Player Login (Client App)
- **Email**: `demo@player.com`
- **Password**: `PlayerPassword123!`

## 🗄️ Database Access

### LocalDB (Windows)
- **Server**: `(localdb)\MSSQLLocalDB`
- **Database**: `ChanzupLocal`
- **Authentication**: Windows Authentication

### Docker SQL Server
- **Server**: `localhost,1433`
- **Database**: `ChanzupLocal`
- **Username**: `sa`
- **Password**: `ChanzupLocal123!`

## 🔧 Configuration Files

### Environment Files
- `.env.local` - Main environment variables
- `src/Chanzup.API/appsettings.Local.json` - API configuration
- `backoffice-app/.env.local` - React back office configuration

### Key Configuration Options

#### Database Connection
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ChanzupLocal;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
  }
}
```

#### JWT Settings
```json
{
  "Jwt": {
    "SecretKey": "your-super-secret-key-that-is-at-least-32-characters-long-for-local-development",
    "Issuer": "Chanzup-Local",
    "Audience": "Chanzup-Local",
    "AccessTokenExpirationMinutes": 60
  }
}
```

#### CORS Settings
```json
{
  "AllowedOrigins": "http://localhost:3000,http://localhost:3001,http://127.0.0.1:3000"
}
```

## 🧪 Testing

### Run Unit Tests
```bash
cd src/Chanzup.Tests
dotnet test
```

### Test API Endpoints
```bash
# Test business login
curl -X POST https://localhost:7001/api/auth/login/business \
  -H "Content-Type: application/json" \
  -d '{"email":"owner@coffeeshop.com","password":"DemoPassword123!"}'

# Test business info (requires token)
curl -X GET https://localhost:7001/api/business/info \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## 🐛 Troubleshooting

### Common Issues

#### 1. Certificate Issues
```bash
# Clear and regenerate certificates
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

#### 2. Database Connection Issues
```bash
# Check LocalDB status
sqllocaldb info MSSQLLocalDB

# Start LocalDB if stopped
sqllocaldb start MSSQLLocalDB
```

#### 3. Port Already in Use
```bash
# Find process using port 7001
lsof -ti:7001

# Kill process (replace PID)
kill -9 PID
```

#### 4. Node Modules Issues
```bash
# Clear and reinstall
rm -rf node_modules package-lock.json
npm install
```

### Debug Mode

#### Enable Detailed Logging
Update `appsettings.Local.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

#### React Debug Mode
```bash
# Start with debug logging
REACT_APP_LOG_LEVEL=debug npm start
```

## 📁 Project Structure

```
chanzup/
├── src/
│   ├── Chanzup.API/          # ASP.NET Core API
│   ├── Chanzup.Application/  # Business Logic
│   ├── Chanzup.Domain/       # Domain Models
│   ├── Chanzup.Infrastructure/ # Data & Services
│   └── Chanzup.Tests/        # Unit Tests
├── backoffice-app/           # React Back Office
├── client-app/               # React Client App
├── scripts/                  # Setup & Launch Scripts
├── .env.local               # Environment Variables
└── docker-compose.local.yml # Docker Configuration
```

## 🔄 Development Workflow

### 1. Make Changes
- Edit code in your preferred IDE
- Hot reload is enabled for React apps
- API requires restart for code changes

### 2. Test Changes
```bash
# Run tests
dotnet test

# Test specific endpoint
curl -X GET https://localhost:7001/api/health
```

### 3. Database Changes
```bash
# Add migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update
```

### 4. Reset Demo Data
```bash
cd src/Chanzup.API
dotnet run --environment Local --seed-demo
```

## 📚 Additional Resources

- [Business Login Implementation Guide](BUSINESS_LOGIN_IMPLEMENTATION.md)
- [API Documentation](https://localhost:7001/swagger) (when running)
- [.NET 8 Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [React Documentation](https://reactjs.org/docs/)

## 🆘 Getting Help

If you encounter issues:

1. Check the troubleshooting section above
2. Review the logs in the terminal
3. Ensure all prerequisites are installed
4. Try the automated setup script
5. Check that all ports are available

## 🎯 Next Steps

After successful setup:

1. **Explore the Back Office**: Login and navigate through the dashboard
2. **Test the Client App**: Try the player experience
3. **Review the Code**: Understand the architecture and patterns
4. **Make Changes**: Start developing new features
5. **Run Tests**: Ensure everything works as expected

Happy coding! 🚀