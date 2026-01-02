# Business Login Implementation

This document describes the implementation of the business login functionality for the Chanzup back office application.

## Overview

The business login system allows business staff (owners and employees) to authenticate and access the back office portal to manage campaigns, view analytics, and handle redemptions.

## Architecture

### Backend (ASP.NET Core API)

**Authentication Flow:**
1. User submits email/password via `/api/auth/login/business`
2. System validates credentials against Staff entity
3. JWT token generated with user claims (role, permissions, tenant ID)
4. Refresh token created for token renewal
5. Response includes access token, refresh token, and user info

**Key Components:**

- **AuthController**: Handles login, logout, and token refresh
- **JwtService**: Generates and validates JWT tokens
- **RefreshTokenService**: Manages refresh token lifecycle
- **PermissionService**: Maps roles to permissions
- **TenantMiddleware**: Extracts tenant context from JWT claims

**Database Entities:**
- **Business**: Represents a business account
- **Staff**: Business employees with roles (Owner, Manager, Staff)
- **RefreshToken**: Stores refresh tokens for secure token renewal

### Frontend (React TypeScript)

**Authentication Flow:**
1. Login form captures email/password
2. API call to `/api/auth/login/business`
3. Tokens stored in localStorage
4. AuthContext provides authentication state
5. Protected routes require authentication
6. Automatic token refresh on API calls

**Key Components:**

- **AuthContext**: Global authentication state management
- **Login**: Login form component
- **ProtectedRoute**: Route guard for authenticated pages
- **Navigation**: Navigation bar with user info and logout
- **ApiClient**: HTTP client with automatic token refresh

## Features Implemented

### ✅ Core Authentication
- [x] Business staff login with email/password
- [x] JWT token generation with claims
- [x] Refresh token mechanism
- [x] Secure token storage
- [x] Automatic token refresh
- [x] Logout functionality

### ✅ Authorization
- [x] Role-based access control (Owner, Manager, Staff)
- [x] Permission-based authorization
- [x] Multi-tenant support (business isolation)
- [x] Protected API endpoints
- [x] Protected React routes

### ✅ Security
- [x] Password hashing with BCrypt
- [x] JWT token validation
- [x] CORS configuration
- [x] Security headers middleware
- [x] Rate limiting
- [x] IP-based security checks

### ✅ User Experience
- [x] Responsive login form
- [x] Loading states and error handling
- [x] Automatic redirect after login
- [x] Navigation with user info
- [x] Dashboard with business metrics

## Demo Data

The system includes demo data for testing:

**Demo Business:**
- Name: Demo Coffee Shop
- Email: demo@coffeeshop.com
- Subscription: Premium

**Demo Staff Account:**
- Email: owner@coffeeshop.com
- Password: DemoPassword123!
- Role: Owner
- Access: Full business management

## API Endpoints

### Authentication
- `POST /api/auth/login/business` - Business staff login
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/logout` - Logout and revoke tokens

### Business Management
- `GET /api/business/info` - Get business information
- `GET /api/business/dashboard/metrics` - Get dashboard metrics

## Setup Instructions

### 1. Database Setup
```bash
cd src/Chanzup.API
dotnet ef database update
```

### 2. Seed Demo Data
```bash
./scripts/setup-demo.sh
```

### 3. Start API Server
```bash
cd src/Chanzup.API
dotnet run
```

### 4. Start React App
```bash
cd backoffice-app
npm install
npm start
```

### 5. Test Login
1. Navigate to http://localhost:3000/login
2. Use demo credentials:
   - Email: owner@coffeeshop.com
   - Password: DemoPassword123!
3. Access dashboard and other protected pages

## Configuration

### JWT Settings (appsettings.json)
```json
{
  "Jwt": {
    "SecretKey": "your-super-secret-key-that-is-at-least-32-characters-long",
    "Issuer": "Chanzup",
    "Audience": "Chanzup",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 30
  }
}
```

### CORS Settings
- Development: Allow all origins
- Production: Configured via AllowedOrigins setting

## Testing

### Unit Tests
```bash
cd src/Chanzup.Tests
dotnet test
```

### Integration Tests
- AuthControllerTests: Tests login endpoints
- Includes in-memory database setup
- Validates token generation and authentication flow

## Security Considerations

1. **Password Security**: BCrypt hashing with salt
2. **Token Security**: Short-lived access tokens (1 hour)
3. **Refresh Tokens**: Secure rotation on each refresh
4. **HTTPS**: Required in production
5. **CORS**: Restricted origins in production
6. **Rate Limiting**: Prevents brute force attacks
7. **Input Validation**: Email format and password requirements

## Future Enhancements

- [ ] Two-factor authentication (2FA)
- [ ] Single sign-on (SSO) integration
- [ ] Password reset functionality
- [ ] Account lockout after failed attempts
- [ ] Audit logging for security events
- [ ] Session management dashboard
- [ ] Mobile app authentication

## Troubleshooting

### Common Issues

1. **Login fails with valid credentials**
   - Check database connection
   - Verify demo data is seeded
   - Check password hashing

2. **Token refresh fails**
   - Check refresh token expiration
   - Verify JWT configuration
   - Check database refresh token storage

3. **CORS errors**
   - Verify CORS configuration
   - Check allowed origins
   - Ensure proper headers

4. **Protected routes not working**
   - Check token storage in localStorage
   - Verify AuthContext provider
   - Check route protection logic

### Debug Commands

```bash
# Check database tables
dotnet ef dbcontext info

# View application logs
dotnet run --environment Development --verbosity detailed

# Test API endpoints
curl -X POST http://localhost:5000/api/auth/login/business \
  -H "Content-Type: application/json" \
  -d '{"email":"owner@coffeeshop.com","password":"DemoPassword123!"}'
```

## Conclusion

The business login functionality is fully implemented with:
- Secure authentication and authorization
- Multi-tenant business isolation
- Modern React frontend with TypeScript
- Comprehensive error handling
- Production-ready security features
- Extensible architecture for future enhancements

The system is ready for production deployment with proper configuration and can be extended with additional features as needed.