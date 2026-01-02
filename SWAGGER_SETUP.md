# 📖 Chanzup API Swagger Documentation Setup

This document describes the comprehensive Swagger/OpenAPI documentation setup for the Chanzup API.

## 🎯 Overview

The Chanzup API includes a fully configured Swagger UI with:
- **Comprehensive API Documentation** - Detailed endpoint descriptions and examples
- **Interactive Testing** - Try out endpoints directly from the browser
- **JWT Authentication Support** - Built-in authentication testing
- **Custom Styling** - Branded UI with Chanzup colors and styling
- **Organized Endpoints** - Grouped by functionality with emoji icons
- **Rich Examples** - Real demo data for easy testing

## 🌐 Access Swagger Documentation

### Local Development
- **URL**: https://localhost:7001/swagger
- **Environment**: Available in Development and Local environments
- **Auto-redirect**: API root (/) redirects to Swagger UI

### Features Available
- ✅ Interactive API testing
- ✅ JWT token authentication
- ✅ Request/response examples
- ✅ Schema documentation
- ✅ Error response examples
- ✅ Demo credentials provided

## 🔐 Authentication in Swagger

### Step 1: Get JWT Token
1. Navigate to **🔐 Authentication** section
2. Expand **POST /api/auth/login/business**
3. Click **"Try it out"**
4. Use demo credentials:
   ```json
   {
     "email": "owner@coffeeshop.com",
     "password": "DemoPassword123!"
   }
   ```
5. Click **"Execute"**
6. Copy the `accessToken` from the response

### Step 2: Authorize Swagger
1. Click the **"Authorize"** button (🔒 icon) at the top
2. Enter: `Bearer YOUR_ACCESS_TOKEN_HERE`
3. Click **"Authorize"**
4. Click **"Close"**

### Step 3: Test Protected Endpoints
- All protected endpoints now work with your token
- Token is automatically included in requests
- Logout revokes the token when done

## 📚 API Documentation Structure

### Endpoint Categories

#### 🔐 Authentication
- **Business Login** - Staff authentication with role-based access
- **Player Login** - Customer authentication for games
- **Admin Login** - System administrator access
- **Registration** - New business and player registration
- **Token Management** - Refresh and logout functionality

#### 🏢 Business Management
- **Business Info** - Company details and settings
- **Dashboard Metrics** - Real-time analytics and KPIs
- **Staff Management** - Employee access and permissions

#### 📢 Campaign Management
- **Campaign CRUD** - Create, read, update, delete campaigns
- **Game Configuration** - Wheel settings and prize setup
- **Location Management** - Multi-location campaign deployment

#### 📊 Analytics & Reporting
- **Performance Metrics** - Campaign effectiveness tracking
- **Player Analytics** - Customer engagement insights
- **Revenue Reports** - Token sales and redemption data

#### 📱 QR Code & Sessions
- **QR Generation** - Location-based QR codes
- **Session Management** - Player game sessions
- **Location Verification** - GPS-based validation

#### 🎰 Game Mechanics
- **Wheel Spinning** - Game execution and results
- **Prize Distribution** - Random prize selection
- **Anti-Fraud** - Duplicate prevention and validation

#### 🎁 Prize Management
- **Prize Configuration** - Prize setup and inventory
- **Redemption Tracking** - Prize claim management
- **Inventory Control** - Stock level monitoring

#### ❤️ Health Checks
- **System Status** - Overall health monitoring
- **Readiness Probes** - Deployment verification
- **Liveness Checks** - Application uptime

## 🎨 Custom Styling Features

### Visual Enhancements
- **Chanzup Branding** - Custom colors and gradients
- **Emoji Icons** - Visual category identification
- **Hover Effects** - Interactive UI elements
- **Responsive Design** - Mobile-friendly interface

### User Experience
- **Organized Layout** - Logical endpoint grouping
- **Rich Examples** - Pre-filled demo data
- **Clear Documentation** - Detailed descriptions and remarks
- **Error Handling** - Comprehensive error response examples

## 🛠️ Configuration Details

### Swagger Generation Settings
```csharp
// XML Documentation
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<DocumentationFile>bin\$(Configuration)\$(TargetFramework)\$(AssemblyName).xml</DocumentationFile>

// Swagger Annotations
[SwaggerOperation(Summary = "...", Description = "...")]
[SwaggerResponse(200, "Success", typeof(ResponseType))]
[SwaggerSchema(Description = "...")]
```

### Security Configuration
```csharp
// JWT Bearer Authentication
options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Name = "Authorization",
    Type = SecuritySchemeType.Http,
    Scheme = "Bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header
});
```

### Custom Filters
- **SwaggerExampleFilter** - Adds realistic example data
- **SwaggerAuthorizationFilter** - Shows auth requirements
- **SwaggerTagDescriptionFilter** - Enhances category descriptions
- **SwaggerEnumSchemaFilter** - Improves enum documentation

## 📋 Demo Data for Testing

### Business Authentication
```json
{
  "email": "owner@coffeeshop.com",
  "password": "DemoPassword123!"
}
```

### Player Authentication
```json
{
  "email": "demo@player.com",
  "password": "PlayerPassword123!"
}
```

### Business Registration Example
```json
{
  "businessName": "My Coffee Shop",
  "email": "owner@mycoffeeshop.com",
  "password": "SecurePassword123!",
  "phone": "+1-555-123-4567",
  "address": "123 Main Street, City, State 12345",
  "latitude": 40.7128,
  "longitude": -74.0060,
  "subscriptionTier": 1
}
```

### Campaign Creation Example
```json
{
  "name": "Summer Promotion",
  "description": "Spin the wheel for summer prizes!",
  "gameType": 0,
  "tokenCostPerSpin": 5,
  "maxSpinsPerDay": 3,
  "startDate": "2024-01-01T00:00:00.000Z",
  "endDate": "2024-12-31T23:59:59.000Z",
  "isActive": true
}
```

## 🔧 Development Setup

### Enable Swagger in Development
```csharp
// Program.cs
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Local")
{
    app.UseSwaggerDocumentation(app.Environment);
}
```

### Add XML Documentation
1. Enable in project file:
   ```xml
   <GenerateDocumentationFile>true</GenerateDocumentationFile>
   ```

2. Add controller documentation:
   ```csharp
   /// <summary>
   /// Endpoint description
   /// </summary>
   /// <param name="request">Parameter description</param>
   /// <returns>Return value description</returns>
   [HttpPost("endpoint")]
   public async Task<IActionResult> Endpoint([FromBody] RequestType request)
   ```

### Custom Styling
1. Create CSS file: `wwwroot/swagger-ui/custom.css`
2. Configure in Swagger setup:
   ```csharp
   options.InjectStylesheet("/swagger-ui/custom.css");
   ```

## 🧪 Testing Workflows

### 1. Authentication Flow
1. **Login** → Get JWT token
2. **Authorize** → Set Bearer token in Swagger
3. **Test Protected Endpoints** → Access business data
4. **Refresh Token** → Extend session
5. **Logout** → Revoke tokens

### 2. Business Management Flow
1. **Get Business Info** → View company details
2. **Dashboard Metrics** → Check performance data
3. **Create Campaign** → Set up new promotion
4. **Manage Prizes** → Configure rewards

### 3. Game Testing Flow
1. **Generate QR Code** → Create location code
2. **Scan QR Code** → Start game session
3. **Spin Wheel** → Play game
4. **Redeem Prize** → Claim reward

## 📊 Response Examples

### Successful Login Response
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "def502004a08b8be09c4b6fd85bb02c09004aeccd...",
  "expiresIn": 3600,
  "tokenType": "Bearer",
  "user": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "email": "owner@coffeeshop.com",
    "role": "BusinessOwner",
    "firstName": "Demo",
    "lastName": "Owner",
    "tenantId": "456e7890-e89b-12d3-a456-426614174001"
  }
}
```

### Error Response Example
```json
{
  "error": "Unauthorized",
  "message": "Invalid email or password"
}
```

### Health Check Response
```json
{
  "status": "Healthy",
  "totalDuration": 45.2,
  "timestamp": "2024-01-01T12:00:00.000Z",
  "version": "1.0.0",
  "entries": [
    {
      "name": "database",
      "status": "Healthy",
      "duration": 23.1,
      "description": "Database connectivity check"
    }
  ]
}
```

## 🚀 Production Considerations

### Security
- Swagger UI disabled in production by default
- JWT tokens have short expiration (1 hour)
- Refresh tokens rotate on each use
- Rate limiting applied to authentication endpoints

### Performance
- Health checks cached for performance
- Minimal overhead for documentation generation
- Efficient JWT validation and caching

### Monitoring
- Health check endpoints for load balancers
- Structured logging for API usage
- Performance metrics collection

## 📖 Additional Resources

### OpenAPI Specification
- **Version**: 3.0.1
- **Format**: JSON and YAML available
- **Download**: Available from Swagger UI

### Related Documentation
- [Business Login Implementation](BUSINESS_LOGIN_IMPLEMENTATION.md)
- [Getting Started Guide](GETTING_STARTED.md)
- [Local Development Setup](README.local.md)

### External Links
- [Swagger/OpenAPI Documentation](https://swagger.io/docs/)
- [Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [JWT.io](https://jwt.io/) - JWT token decoder

## 🎉 Conclusion

The Chanzup API Swagger documentation provides a comprehensive, interactive way to explore and test the API. With built-in authentication, rich examples, and custom styling, it serves as both documentation and a powerful testing tool for developers and stakeholders.

**Quick Start:**
1. Navigate to https://localhost:7001/swagger
2. Login with demo credentials
3. Authorize with JWT token
4. Explore and test endpoints
5. Build amazing applications! 🚀