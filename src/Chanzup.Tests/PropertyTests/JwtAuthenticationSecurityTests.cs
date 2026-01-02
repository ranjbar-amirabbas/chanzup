using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Chanzup.Application.DTOs;
using Chanzup.Domain.Entities;
using Chanzup.Domain.ValueObjects;
using Chanzup.Infrastructure.Data;
using Chanzup.Infrastructure.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace Chanzup.Tests.PropertyTests;

/// <summary>
/// Feature: vancouver-rewards-platform, Property 7: JWT Authentication Security
/// Validates: Requirements 3.2, 3.3
/// </summary>
public class JwtAuthenticationSecurityTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var tenantContext = new TenantContext();
        return new ApplicationDbContext(options, tenantContext);
    }

    private IConfiguration CreateTestConfiguration()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "test-secret-key-that-is-at-least-32-characters-long",
            ["Jwt:Issuer"] = "TestChanzup",
            ["Jwt:Audience"] = "TestChanzup",
            ["Jwt:AccessTokenExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "30"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    [Property(MaxTest = 25)]
    public Property ValidCredentialsGenerateSecureTokens()
    {
        return Prop.ForAll(
            GenerateValidUserCredentials(),
            (credentials) =>
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var jwtService = new JwtService(CreateTestConfiguration());
                var refreshTokenService = new RefreshTokenService(context, jwtService);
                var permissionService = new PermissionService();

                // Create user account based on type
                Guid userId;
                string role;
                Guid? tenantId = null;
                List<string> permissions;

                if (credentials.UserType == "Player")
                {
                    var player = new Player
                    {
                        Email = new Email(credentials.Email),
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(credentials.Password),
                        FirstName = "Test",
                        LastName = "Player"
                    };
                    context.Players.Add(player);
                    context.SaveChanges();
                    
                    userId = player.Id;
                    role = "Player";
                    permissions = permissionService.GetPermissionsForRole(role);
                }
                else
                {
                    var business = new Business
                    {
                        Name = "Test Business",
                        Email = new Email(credentials.Email),
                        SubscriptionTier = SubscriptionTier.Basic
                    };
                    context.Businesses.Add(business);
                    context.SaveChanges();

                    var staff = new Staff
                    {
                        BusinessId = business.Id,
                        Email = new Email(credentials.Email),
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(credentials.Password),
                        FirstName = "Test",
                        LastName = "Owner",
                        Role = StaffRole.Owner
                    };
                    context.Staff.Add(staff);
                    context.SaveChanges();
                    
                    userId = staff.Id;
                    role = "BusinessOwner";
                    tenantId = business.Id;
                    permissions = permissionService.GetPermissionsForRole(role, StaffRole.Owner);
                }

                // Act - Generate tokens
                var accessToken = jwtService.GenerateToken(userId, credentials.Email, role, tenantId, permissions);
                var refreshTokenTask = refreshTokenService.CreateRefreshTokenAsync(userId, credentials.UserType, "127.0.0.1");
                refreshTokenTask.Wait();
                var refreshToken = refreshTokenTask.Result;

                // Validate access token
                var principal = jwtService.ValidateToken(accessToken);
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(accessToken);

                // Assert - Verify token security properties
                var hasValidStructure = principal != null &&
                                      !string.IsNullOrEmpty(accessToken) &&
                                      jwtToken.Claims.Any();

                var hasCorrectClaims = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value == userId.ToString() &&
                                     principal.FindFirst(ClaimTypes.Email)?.Value == credentials.Email &&
                                     principal.FindFirst(ClaimTypes.Role)?.Value == role;

                var hasPermissions = permissions.All(p => 
                    principal.FindAll("permission").Select(c => c.Value).Contains(p));

                var hasTenantIdWhenRequired = (role == "Player") || 
                    (tenantId.HasValue && principal.FindFirst("tenantId")?.Value == tenantId.ToString());

                var refreshTokenIsValid = refreshToken != null &&
                                        !string.IsNullOrEmpty(refreshToken.Token) &&
                                        refreshToken.UserId == userId &&
                                        refreshToken.UserType == credentials.UserType &&
                                        refreshToken.IsActive &&
                                        refreshToken.ExpiresAt > DateTime.UtcNow;

                return hasValidStructure &&
                       hasCorrectClaims &&
                       hasPermissions &&
                       hasTenantIdWhenRequired &&
                       refreshTokenIsValid;
            });
    }

    [Property(MaxTest = 25)]
    public Property InvalidTokensAreRejected()
    {
        return Prop.ForAll(
            GenerateInvalidTokens(),
            (invalidToken) =>
            {
                // Arrange
                var jwtService = new JwtService(CreateTestConfiguration());

                // Act - Try to validate invalid token
                var principal = jwtService.ValidateToken(invalidToken);

                // Assert - Invalid tokens should be rejected
                return principal == null;
            });
    }

    [Property(MaxTest = 25)]
    public Property ExpiredTokensAreRejected()
    {
        return Prop.ForAll(
            GenerateValidUserCredentials(),
            (credentials) =>
            {
                // Arrange
                var expiredConfig = new Dictionary<string, string?>
                {
                    ["Jwt:SecretKey"] = "test-secret-key-that-is-at-least-32-characters-long",
                    ["Jwt:Issuer"] = "TestChanzup",
                    ["Jwt:Audience"] = "TestChanzup",
                    ["Jwt:AccessTokenExpirationMinutes"] = "-1" // Expired immediately
                };

                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(expiredConfig)
                    .Build();

                var jwtService = new JwtService(config);
                var userId = Guid.NewGuid();
                var permissions = new List<string> { "test:permission" };

                // Act - Generate expired token
                var expiredToken = jwtService.GenerateToken(userId, credentials.Email, "Player", null, permissions);
                
                // Wait a moment to ensure expiration
                Thread.Sleep(100);
                
                var principal = jwtService.ValidateToken(expiredToken);

                // Assert - Expired tokens should be rejected
                return principal == null;
            });
    }

    [Property(MaxTest = 25)]
    public Property RefreshTokenRotationWorks()
    {
        return Prop.ForAll(
            GenerateValidUserCredentials(),
            (credentials) =>
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var jwtService = new JwtService(CreateTestConfiguration());
                var refreshTokenService = new RefreshTokenService(context, jwtService);
                var userId = Guid.NewGuid();

                // Act - Create initial refresh token
                var initialTokenTask = refreshTokenService.CreateRefreshTokenAsync(userId, credentials.UserType, "127.0.0.1");
                initialTokenTask.Wait();
                var initialToken = initialTokenTask.Result;

                // Revoke and create new token (simulating refresh)
                var revokeTask = refreshTokenService.RevokeRefreshTokenAsync(initialToken.Token, "127.0.0.1", "new-token");
                revokeTask.Wait();

                var newTokenTask = refreshTokenService.CreateRefreshTokenAsync(userId, credentials.UserType, "127.0.0.1");
                newTokenTask.Wait();
                var newToken = newTokenTask.Result;

                // Verify old token is revoked
                var oldTokenTask = refreshTokenService.GetRefreshTokenAsync(initialToken.Token);
                oldTokenTask.Wait();
                var revokedToken = oldTokenTask.Result;

                // Assert - Token rotation properties
                return revokedToken != null &&
                       revokedToken.IsRevoked &&
                       !revokedToken.IsActive &&
                       newToken != null &&
                       newToken.IsActive &&
                       newToken.Token != initialToken.Token &&
                       newToken.UserId == userId;
            });
    }

    [Property(MaxTest = 25)]
    public Property PermissionClaimsAreAccurate()
    {
        return Prop.ForAll(
            GenerateRoleWithPermissions(),
            (roleData) =>
            {
                // Arrange
                var jwtService = new JwtService(CreateTestConfiguration());
                var permissionService = new PermissionService();
                var userId = Guid.NewGuid();
                var email = "test@example.com";

                // Get expected permissions for role
                var expectedPermissions = permissionService.GetPermissionsForRole(roleData.Role, roleData.StaffRole);

                // Act - Generate token with permissions
                var token = jwtService.GenerateToken(userId, email, roleData.Role, roleData.TenantId, expectedPermissions);
                var principal = jwtService.ValidateToken(token);

                // Assert - All expected permissions are present in token
                if (principal == null) return false;

                var tokenPermissions = principal.FindAll("permission").Select(c => c.Value).ToList();
                
                return expectedPermissions.All(p => tokenPermissions.Contains(p)) &&
                       tokenPermissions.Count == expectedPermissions.Count;
            });
    }

    private static Arbitrary<UserCredentials> GenerateValidUserCredentials()
    {
        return Arb.From(
            from userType in Gen.Elements("Player", "Staff")
            from email in GenerateValidEmail()
            from password in GenerateValidPassword()
            select new UserCredentials
            {
                UserType = userType,
                Email = email,
                Password = password
            });
    }

    private static Arbitrary<string> GenerateInvalidTokens()
    {
        return Arb.From(Gen.OneOf(
            Gen.Constant(""),
            Gen.Constant("invalid.token.here"),
            Gen.Constant("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalid.signature"),
            Gen.Constant("not-a-jwt-token"),
            Gen.Constant("Bearer token-without-proper-format")
        ));
    }

    private static Arbitrary<RolePermissionData> GenerateRoleWithPermissions()
    {
        return Arb.From(
            from role in Gen.Elements("Admin", "BusinessOwner", "Staff", "Player")
            from staffRole in Gen.Elements(StaffRole.Staff, StaffRole.Manager, StaffRole.Owner)
            from tenantId in Arb.Generate<Guid?>()
            select new RolePermissionData
            {
                Role = role,
                StaffRole = role == "Staff" || role == "BusinessOwner" ? staffRole : (StaffRole?)null,
                TenantId = role == "BusinessOwner" || role == "Staff" ? tenantId : null
            });
    }

    private static Gen<string> GenerateValidEmail()
    {
        return from localPart in Gen.Elements("user", "test", "admin", "player", "business")
               from domain in Gen.Elements("example.com", "test.org", "demo.net")
               select $"{localPart}@{domain}";
    }

    private static Gen<string> GenerateValidPassword()
    {
        return Gen.Elements("Password123!", "SecurePass456!", "TestPassword789!");
    }

    public class UserCredentials
    {
        public string UserType { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RolePermissionData
    {
        public string Role { get; set; } = string.Empty;
        public StaffRole? StaffRole { get; set; }
        public Guid? TenantId { get; set; }
    }
}