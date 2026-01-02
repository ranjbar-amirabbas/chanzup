using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Chanzup.Application.DTOs;
using Chanzup.Domain.Entities;
using Chanzup.Domain.ValueObjects;
using Chanzup.Infrastructure.Data;
using Chanzup.Infrastructure.Services;
using Xunit;

namespace Chanzup.Tests.PropertyTests;

/// <summary>
/// Feature: vancouver-rewards-platform, Property 1: Account Creation Consistency
/// Validates: Requirements 1.1, 3.1
/// </summary>
public class AccountCreationConsistencyTests
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
            ["Jwt:Audience"] = "TestChanzup"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    [Property(MaxTest = 25)]
    public Property BusinessAccountCreationConsistency()
    {
        return Prop.ForAll(
            GenerateValidBusinessRegistration(),
            (request) =>
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var jwtService = new JwtService(CreateTestConfiguration());

                // Act - Create business account
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                
                var business = new Business
                {
                    Name = request.BusinessName,
                    Email = new Email(request.Email),
                    Phone = request.Phone,
                    Address = request.Address,
                    SubscriptionTier = (SubscriptionTier)request.SubscriptionTier
                };

                // Set location if provided
                if (request.Latitude.HasValue && request.Longitude.HasValue)
                {
                    business.UpdateLocation(request.Latitude.Value, request.Longitude.Value);
                }

                context.Businesses.Add(business);
                context.SaveChanges();

                var staff = new Staff
                {
                    BusinessId = business.Id,
                    Email = new Email(request.Email),
                    PasswordHash = passwordHash,
                    FirstName = "Business",
                    LastName = "Owner",
                    Role = StaffRole.Owner
                };

                context.Staff.Add(staff);
                context.SaveChanges();

                // Generate JWT token
                var token = jwtService.GenerateToken(staff.Id, staff.Email.Value, "BusinessOwner", business.Id);

                // Assert - Verify account creation consistency
                var createdBusiness = context.Businesses.FirstOrDefault(b => b.Email.Value == request.Email);
                var createdStaff = context.Staff.FirstOrDefault(s => s.Email.Value == request.Email);

                return (createdBusiness != null) &&
                       (createdStaff != null) &&
                       (createdBusiness.Name == request.BusinessName) &&
                       (createdBusiness.Email.Value == request.Email) &&
                       (createdStaff.Role == StaffRole.Owner) &&
                       (createdStaff.BusinessId == business.Id) &&
                       (!string.IsNullOrEmpty(token)) &&
                       (createdBusiness.IsActive == true) &&
                       (createdBusiness.SubscriptionTier == (SubscriptionTier)request.SubscriptionTier);
            });
    }

    [Property(MaxTest = 25)]
    public Property PlayerAccountCreationConsistency()
    {
        return Prop.ForAll(
            GenerateValidPlayerRegistration(),
            (request) =>
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var jwtService = new JwtService(CreateTestConfiguration());

                // Act - Create player account
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                
                var player = new Player
                {
                    Email = new Email(request.Email),
                    PasswordHash = passwordHash,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Phone = request.Phone
                };

                context.Players.Add(player);
                context.SaveChanges();

                // Generate JWT token
                var token = jwtService.GenerateToken(player.Id, player.Email.Value, "Player");

                // Assert - Verify account creation consistency
                var createdPlayer = context.Players.FirstOrDefault(p => p.Email.Value == request.Email);

                return (createdPlayer != null) &&
                       (createdPlayer.Email.Value == request.Email) &&
                       (createdPlayer.FirstName == request.FirstName) &&
                       (createdPlayer.LastName == request.LastName) &&
                       (createdPlayer.Phone == request.Phone) &&
                       (!string.IsNullOrEmpty(token)) &&
                       (createdPlayer.IsActive == true) &&
                       (createdPlayer.TokenBalance == 0);
            });
    }

    private static Arbitrary<RegisterBusinessRequest> GenerateValidBusinessRegistration()
    {
        return Arb.From(
            from businessName in Arb.Generate<NonEmptyString>()
            from email in GenerateValidEmail()
            from password in GenerateValidPassword()
            from phone in Arb.Generate<string>()
            from address in Arb.Generate<string>()
            from latitude in Gen.Choose(-90, 90).Select(x => (decimal?)x)
            from longitude in Gen.Choose(-180, 180).Select(x => (decimal?)x)
            from subscriptionTier in Gen.Choose(0, 2)
            select new RegisterBusinessRequest
            {
                BusinessName = businessName.Get,
                Email = email,
                Password = password,
                Phone = phone,
                Address = address,
                Latitude = latitude,
                Longitude = longitude,
                SubscriptionTier = subscriptionTier
            });
    }

    private static Arbitrary<RegisterPlayerRequest> GenerateValidPlayerRegistration()
    {
        return Arb.From(
            from email in GenerateValidEmail()
            from password in GenerateValidPassword()
            from firstName in Arb.Generate<string>()
            from lastName in Arb.Generate<string>()
            from phone in Arb.Generate<string>()
            select new RegisterPlayerRequest
            {
                Email = email,
                Password = password,
                FirstName = firstName,
                LastName = lastName,
                Phone = phone
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
}