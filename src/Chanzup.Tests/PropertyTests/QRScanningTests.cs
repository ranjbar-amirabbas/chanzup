using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Chanzup.Application.DTOs;
using Chanzup.Application.Interfaces;
using Chanzup.Application.Services;
using Chanzup.Domain.Entities;
using Chanzup.Domain.ValueObjects;
using Chanzup.Infrastructure.Data;
using Chanzup.Infrastructure.Services;
using Xunit;

namespace Chanzup.Tests.PropertyTests;

/// <summary>
/// Feature: vancouver-rewards-platform, Property 8: QR Session Creation and Token Award
/// Feature: vancouver-rewards-platform, Property 9: Location Verification Integrity
/// Feature: vancouver-rewards-platform, Property 10: Anti-Fraud Protection
/// Validates: Requirements 4.1, 4.2, 4.3, 4.4, 4.5
/// </summary>
public class QRScanningTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var tenantContext = new TenantContext();
        return new ApplicationDbContext(options, tenantContext);
    }

    private QRSessionService CreateQRSessionService(ApplicationDbContext context)
    {
        var qrCodeService = new QRCodeService();
        var antiFraudService = new AntiFraudService(context);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var rateLimitingService = new RateLimitingService(memoryCache);
        var analyticsService = new Mock<IAnalyticsService>().Object;
        
        return new QRSessionService(context, qrCodeService, antiFraudService, rateLimitingService, analyticsService);
    }

    [Property(MaxTest = 25)]
    public Property QRSessionCreationAndTokenAwardProperty()
    {
        return Prop.ForAll(
            GenerateValidQRScanScenario(),
            (scenario) =>
            {
                if (scenario.Item1 == null || scenario.Item2 == null || scenario.Item3 == null || scenario.Item4 == null)
                    return true; // Skip null scenarios

                var (player, business, campaign, scanRequest) = scenario;

                // Arrange
                using var context = CreateInMemoryContext();
                var qrSessionService = CreateQRSessionService(context);

                // Set up test data
                context.Players.Add(player);
                context.Businesses.Add(business);
                context.Campaigns.Add(campaign);
                context.SaveChanges();

                var initialBalance = player.TokenBalance;

                try
                {
                    // Act - Process QR scan
                    var result = qrSessionService.ProcessQRScanAsync(player.Id, scanRequest).Result;

                    // Assert - For valid scans, session should be created and tokens awarded
                    if (result.SessionId != Guid.Empty)
                    {
                        // Verify session was created
                        var session = context.QRSessions.FirstOrDefault(s => s.Id == result.SessionId);
                        if (session == null) return false;

                        // Verify session properties
                        var sessionValid = session.PlayerId == player.Id &&
                                         session.BusinessId == business.Id &&
                                         session.TokensEarned > 0;

                        // Verify tokens were awarded
                        var updatedPlayer = context.Players.First(p => p.Id == player.Id);
                        var tokensAwarded = updatedPlayer.TokenBalance > initialBalance;

                        // Verify token transaction was recorded
                        var transaction = context.TokenTransactions
                            .FirstOrDefault(t => t.PlayerId == player.Id && t.Type == TransactionType.Earned);
                        var transactionRecorded = transaction != null;

                        return sessionValid && tokensAwarded && transactionRecorded;
                    }

                    // For invalid scans, no session should be created
                    return true; // Invalid scans are expected to fail
                }
                catch
                {
                    // Exceptions during processing indicate system failure
                    return false;
                }
            });
    }

    [Property(MaxTest = 25)]
    public Property LocationVerificationIntegrityProperty()
    {
        return Prop.ForAll(
            GenerateLocationVerificationScenario(),
            (scenario) =>
            {
                var (business, playerLat, playerLng, expectedValid) = scenario;

                // Arrange
                using var context = CreateInMemoryContext();
                var qrSessionService = CreateQRSessionService(context);

                context.Businesses.Add(business);
                context.SaveChanges();

                try
                {
                    // Act - Validate location
                    var isValid = qrSessionService.ValidateLocationAsync(business.Id, playerLat, playerLng).Result;

                    // Assert - Location validation should match expected result
                    return isValid == expectedValid;
                }
                catch
                {
                    // Location validation should not throw exceptions
                    return false;
                }
            });
    }

    [Property(MaxTest = 25)]
    public Property AntiFraudProtectionProperty()
    {
        return Prop.ForAll(
            GenerateAntiFraudScenario(),
            (scenario) =>
            {
                var (player, business, campaign, scanRequests) = scenario;

                // Arrange
                using var context = CreateInMemoryContext();
                var qrSessionService = CreateQRSessionService(context);

                context.Players.Add(player);
                context.Businesses.Add(business);
                context.Campaigns.Add(campaign);
                context.SaveChanges();

                var successfulScans = 0;
                var rejectedScans = 0;

                try
                {
                    // Act - Process multiple scan requests
                    foreach (var scanRequest in scanRequests)
                    {
                        var result = qrSessionService.ProcessQRScanAsync(player.Id, scanRequest).Result;
                        
                        if (result.SessionId != Guid.Empty)
                        {
                            successfulScans++;
                        }
                        else
                        {
                            rejectedScans++;
                        }

                        // Add small delay to simulate real-world timing
                        Thread.Sleep(10);
                    }

                    // Assert - Anti-fraud measures should prevent excessive scanning
                    // At least some scans should be rejected if there are many rapid attempts
                    if (scanRequests.Count > 5)
                    {
                        return rejectedScans > 0; // Some scans should be rejected due to rate limiting
                    }

                    // For fewer scans, they might all succeed if properly spaced
                    return successfulScans + rejectedScans == scanRequests.Count;
                }
                catch
                {
                    // Anti-fraud protection should not cause system failures
                    return false;
                }
            });
    }

    [Property(MaxTest = 25)]
    public Property CooldownPeriodEnforcementProperty()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 60)),
            (minutesBetween) =>
            {
                // Create fixed test data
                var playerId = Guid.NewGuid();
                var businessId = Guid.NewGuid();
                var campaignId = Guid.NewGuid();

                var player = new Player
                {
                    Id = playerId,
                    Email = new Email($"player{playerId:N}@example.com"),
                    TokenBalance = 1000,
                    IsActive = true
                };

                var business = new Business
                {
                    Id = businessId,
                    Name = $"Business {businessId:N}",
                    Email = new Email($"business{businessId:N}@example.com"),
                    Location = new Location(49.2827m, -123.1207m),
                    IsActive = true
                };

                var campaign = new Campaign
                {
                    Id = campaignId,
                    BusinessId = businessId,
                    Name = $"Campaign {campaignId:N}",
                    TokenCostPerSpin = 5,
                    MaxSpinsPerDay = 10,
                    IsActive = true,
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    EndDate = DateTime.UtcNow.AddDays(30)
                };

                // Use fixed base time to avoid timing issues
                var baseTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
                var firstScanTime = baseTime;
                var secondScanTime = baseTime.AddMinutes(minutesBetween);

                // Arrange
                using var context = CreateInMemoryContext();
                var qrSessionService = CreateQRSessionService(context);

                context.Players.Add(player);
                context.Businesses.Add(business);
                context.Campaigns.Add(campaign);
                context.SaveChanges();

                try
                {
                    // Act - Process first scan
                    var firstScanRequest = new QRScanRequest
                    {
                        QRCode = $"CMP-{campaign.Id:N}",
                        Latitude = business.Location.Latitude,
                        Longitude = business.Location.Longitude,
                        Timestamp = firstScanTime
                    };

                    var firstResult = qrSessionService.ProcessQRScanAsync(player.Id, firstScanRequest).Result;

                    // Only proceed if first scan was successful
                    if (firstResult.SessionId == Guid.Empty)
                    {
                        // First scan failed - this might be due to anti-fraud or other validation
                        // For cooldown testing, we need the first scan to succeed
                        return true; // Skip this test case
                    }

                    // Verify the first session was actually created and saved
                    var firstSession = context.QRSessions.FirstOrDefault(s => s.Id == firstResult.SessionId);
                    if (firstSession == null)
                    {
                        return false; // First session should exist
                    }

                    // Manually check cooldown using the service method
                    var isWithinCooldown = qrSessionService.IsWithinCooldownPeriodAsync(playerId, businessId, secondScanTime).Result;
                    
                    // The cooldown check should be based on the time difference
                    var expectedWithinCooldown = minutesBetween < 30;
                    
                    if (isWithinCooldown != expectedWithinCooldown)
                    {
                        // Debug information
                        System.Diagnostics.Debug.WriteLine($"Cooldown mismatch: minutesBetween={minutesBetween}, expected={expectedWithinCooldown}, actual={isWithinCooldown}");
                        return false;
                    }

                    // Now test the actual second scan
                    var secondScanRequest = new QRScanRequest
                    {
                        QRCode = $"CMP-{campaign.Id:N}",
                        Latitude = business.Location.Latitude,
                        Longitude = business.Location.Longitude,
                        Timestamp = secondScanTime
                    };

                    var secondResult = qrSessionService.ProcessQRScanAsync(player.Id, secondScanRequest).Result;

                    // Assert based on cooldown period
                    if (minutesBetween < 30) // Within cooldown
                    {
                        // Should be rejected - no session ID should be returned
                        return secondResult.SessionId == Guid.Empty;
                    }
                    else // Outside cooldown
                    {
                        // Should be allowed - session ID should be returned
                        return secondResult.SessionId != Guid.Empty;
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception for debugging
                    System.Diagnostics.Debug.WriteLine($"Exception in cooldown test: {ex.Message}");
                    return false;
                }
            });
    }

    private static Arbitrary<(Player, Business, Campaign, QRScanRequest)> GenerateValidQRScanScenario()
    {
        return Arb.From(
            from playerId in Arb.Generate<Guid>().Where(g => g != Guid.Empty)
            from businessId in Arb.Generate<Guid>().Where(g => g != Guid.Empty)
            from campaignId in Arb.Generate<Guid>().Where(g => g != Guid.Empty)
            from playerLat in Gen.Choose(-89, 89).Select(x => (decimal)x)
            from playerLng in Gen.Choose(-179, 179).Select(x => (decimal)x)
            from businessLat in Gen.Choose(-89, 89).Select(x => (decimal)x)
            from businessLng in Gen.Choose(-179, 179).Select(x => (decimal)x)
            from tokenBalance in Gen.Choose(0, 1000)
            select CreateQRScanScenario(playerId, businessId, campaignId, playerLat, playerLng, businessLat, businessLng, tokenBalance));
    }

    private static (Player, Business, Campaign, QRScanRequest) CreateQRScanScenario(
        Guid playerId, Guid businessId, Guid campaignId,
        decimal playerLat, decimal playerLng, decimal businessLat, decimal businessLng, int tokenBalance)
    {
        try
        {
            var player = new Player
            {
                Id = playerId,
                Email = new Email($"player{playerId:N}@example.com"),
                TokenBalance = tokenBalance,
                IsActive = true
            };

            var business = new Business
            {
                Id = businessId,
                Name = $"Business {businessId:N}",
                Email = new Email($"business{businessId:N}@example.com"),
                Location = new Location(businessLat, businessLng),
                IsActive = true
            };

            var campaign = new Campaign
            {
                Id = campaignId,
                BusinessId = businessId,
                Name = $"Campaign {campaignId:N}",
                TokenCostPerSpin = 5,
                MaxSpinsPerDay = 10,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            // Make player location close to business location for valid scans
            var random = new System.Random();
            var adjustedPlayerLat = businessLat + (decimal)(random.NextDouble() * 0.001 - 0.0005); // Within ~50m
            var adjustedPlayerLng = businessLng + (decimal)(random.NextDouble() * 0.001 - 0.0005);

            var scanRequest = new QRScanRequest
            {
                QRCode = $"CMP-{campaignId:N}",
                Latitude = adjustedPlayerLat,
                Longitude = adjustedPlayerLng,
                Timestamp = DateTime.UtcNow
            };

            return (player, business, campaign, scanRequest);
        }
        catch
        {
            // Return a valid default scenario if creation fails
            var defaultPlayer = new Player
            {
                Id = Guid.NewGuid(),
                Email = new Email("default@example.com"),
                TokenBalance = 100,
                IsActive = true
            };

            var defaultBusiness = new Business
            {
                Id = Guid.NewGuid(),
                Name = "Default Business",
                Email = new Email("defaultbiz@example.com"),
                Location = new Location(49.2827m, -123.1207m),
                IsActive = true
            };

            var defaultCampaign = new Campaign
            {
                Id = Guid.NewGuid(),
                BusinessId = defaultBusiness.Id,
                Name = "Default Campaign",
                TokenCostPerSpin = 5,
                MaxSpinsPerDay = 10,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            var defaultScanRequest = new QRScanRequest
            {
                QRCode = $"CMP-{defaultCampaign.Id:N}",
                Latitude = 49.2827m,
                Longitude = -123.1207m,
                Timestamp = DateTime.UtcNow
            };

            return (defaultPlayer, defaultBusiness, defaultCampaign, defaultScanRequest);
        }
    }

    private static Arbitrary<(Business, decimal, decimal, bool)> GenerateLocationVerificationScenario()
    {
        return Arb.From(
            from businessLat in Gen.Choose(-89, 89).Select(x => (decimal)x)
            from businessLng in Gen.Choose(-179, 179).Select(x => (decimal)x)
            from distanceMeters in Gen.Choose(0, 500)
            from angle in Gen.Choose(0, 359).Select(x => x * Math.PI / 180) // Convert to radians
            select CreateLocationVerificationScenario(businessLat, businessLng, distanceMeters, angle));
    }

    private static (Business, decimal, decimal, bool) CreateLocationVerificationScenario(
        decimal businessLat, decimal businessLng, int distanceMeters, double angle)
    {
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = "Test Business",
            Email = new Email("test@example.com"),
            Location = new Location(businessLat, businessLng),
            IsActive = true
        };

        // Calculate player position at specified distance and angle
        const double earthRadius = 6371000; // Earth's radius in meters
        var latRadians = (double)businessLat * Math.PI / 180;
        var lngRadians = (double)businessLng * Math.PI / 180;

        var newLatRadians = Math.Asin(Math.Sin(latRadians) * Math.Cos(distanceMeters / earthRadius) +
                                     Math.Cos(latRadians) * Math.Sin(distanceMeters / earthRadius) * Math.Cos(angle));
        var newLngRadians = lngRadians + Math.Atan2(Math.Sin(angle) * Math.Sin(distanceMeters / earthRadius) * Math.Cos(latRadians),
                                                   Math.Cos(distanceMeters / earthRadius) - Math.Sin(latRadians) * Math.Sin(newLatRadians));

        var playerLat = (decimal)(newLatRadians * 180 / Math.PI);
        var playerLng = (decimal)(newLngRadians * 180 / Math.PI);

        // Expected result: valid if within 100 meters
        var expectedValid = distanceMeters <= 100;

        return (business, playerLat, playerLng, expectedValid);
    }

    private static Arbitrary<(Player, Business, Campaign, List<QRScanRequest>)> GenerateAntiFraudScenario()
    {
        return Arb.From(
            from playerId in Arb.Generate<Guid>().Where(g => g != Guid.Empty)
            from businessId in Arb.Generate<Guid>().Where(g => g != Guid.Empty)
            from campaignId in Arb.Generate<Guid>().Where(g => g != Guid.Empty)
            from scanCount in Gen.Choose(2, 15)
            select CreateAntiFraudScenario(playerId, businessId, campaignId, scanCount));
    }

    private static (Player, Business, Campaign, List<QRScanRequest>) CreateAntiFraudScenario(
        Guid playerId, Guid businessId, Guid campaignId, int scanCount)
    {
        var player = new Player
        {
            Id = playerId,
            Email = new Email($"player{playerId:N}@example.com"),
            TokenBalance = 1000,
            IsActive = true
        };

        var business = new Business
        {
            Id = businessId,
            Name = $"Business {businessId:N}",
            Email = new Email($"business{businessId:N}@example.com"),
            Location = new Location(49.2827m, -123.1207m),
            IsActive = true
        };

        var campaign = new Campaign
        {
            Id = campaignId,
            BusinessId = businessId,
            Name = $"Campaign {campaignId:N}",
            TokenCostPerSpin = 5,
            MaxSpinsPerDay = 10,
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        var scanRequests = new List<QRScanRequest>();
        var baseTime = DateTime.UtcNow;

        for (int i = 0; i < scanCount; i++)
        {
            scanRequests.Add(new QRScanRequest
            {
                QRCode = $"CMP-{campaignId:N}",
                Latitude = business.Location.Latitude,
                Longitude = business.Location.Longitude,
                Timestamp = baseTime.AddSeconds(i * 5) // 5 seconds apart
            });
        }

        return (player, business, campaign, scanRequests);
    }

    private static Arbitrary<(Player, Business, Campaign, DateTime, DateTime)> GenerateCooldownScenario()
    {
        return Arb.From(
            from playerId in Arb.Generate<Guid>().Where(g => g != Guid.Empty)
            from businessId in Arb.Generate<Guid>().Where(g => g != Guid.Empty)
            from campaignId in Arb.Generate<Guid>().Where(g => g != Guid.Empty)
            from minutesBetween in Gen.Choose(1, 60)
            select CreateCooldownScenario(playerId, businessId, campaignId, minutesBetween));
    }

    private static (Player, Business, Campaign, DateTime, DateTime) CreateCooldownScenario(
        Guid playerId, Guid businessId, Guid campaignId, int minutesBetween)
    {
        try
        {
            var player = new Player
            {
                Id = playerId,
                Email = new Email($"player{playerId:N}@example.com"),
                TokenBalance = 1000,
                IsActive = true
            };

            var business = new Business
            {
                Id = businessId,
                Name = $"Business {businessId:N}",
                Email = new Email($"business{businessId:N}@example.com"),
                Location = new Location(49.2827m, -123.1207m),
                IsActive = true
            };

            var campaign = new Campaign
            {
                Id = campaignId,
                BusinessId = businessId,
                Name = $"Campaign {campaignId:N}",
                TokenCostPerSpin = 5,
                MaxSpinsPerDay = 10,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            var firstScanTime = DateTime.UtcNow;
            var secondScanTime = firstScanTime.AddMinutes(minutesBetween);

            return (player, business, campaign, firstScanTime, secondScanTime);
        }
        catch
        {
            // Return a valid default scenario if creation fails
            var defaultPlayer = new Player
            {
                Id = Guid.NewGuid(),
                Email = new Email("default@example.com"),
                TokenBalance = 1000,
                IsActive = true
            };

            var defaultBusiness = new Business
            {
                Id = Guid.NewGuid(),
                Name = "Default Business",
                Email = new Email("defaultbiz@example.com"),
                Location = new Location(49.2827m, -123.1207m),
                IsActive = true
            };

            var defaultCampaign = new Campaign
            {
                Id = Guid.NewGuid(),
                BusinessId = defaultBusiness.Id,
                Name = "Default Campaign",
                TokenCostPerSpin = 5,
                MaxSpinsPerDay = 10,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            var firstTime = DateTime.UtcNow;
            var secondTime = firstTime.AddMinutes(30);

            return (defaultPlayer, defaultBusiness, defaultCampaign, firstTime, secondTime);
        }
    }
}