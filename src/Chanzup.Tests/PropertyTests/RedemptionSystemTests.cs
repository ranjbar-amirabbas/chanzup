using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Chanzup.Application.Services;
using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;
using Chanzup.Domain.ValueObjects;
using Chanzup.Infrastructure.Data;
using Chanzup.Infrastructure.Services;
using Xunit;

namespace Chanzup.Tests.PropertyTests;

/// <summary>
/// Feature: vancouver-rewards-platform, Property 15: Redemption Workflow Integrity
/// Feature: vancouver-rewards-platform, Property 16: Prize Expiration Management
/// Validates: Requirements 6.1, 6.2, 6.3, 6.4, 6.6
/// </summary>
public class RedemptionSystemTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var tenantContext = new TenantContext();
        return new ApplicationDbContext(options, tenantContext);
    }

    private (Player player, Business business, Campaign campaign, Prize prize, Staff staff) CreateTestData(ApplicationDbContext context)
    {
        // Create business
        var business = new Business
        {
            Name = $"Test Business {Guid.NewGuid()}",
            Email = new Email($"business{Guid.NewGuid()}@example.com"),
            Address = "123 Test St",
            IsActive = true
        };
        context.Businesses.Add(business);

        // Create player
        var player = new Player
        {
            Email = new Email($"player{Guid.NewGuid()}@example.com"),
            PasswordHash = "hashedpassword",
            FirstName = "Test",
            LastName = "Player",
            TokenBalance = 100,
            IsActive = true
        };
        context.Players.Add(player);

        // Create campaign
        var campaign = new Campaign
        {
            BusinessId = business.Id,
            Name = "Test Campaign",
            Description = "Test campaign for redemption",
            GameType = GameType.WheelOfLuck,
            TokenCostPerSpin = 5,
            MaxSpinsPerDay = 10,
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        context.Campaigns.Add(campaign);

        // Create prize
        var prize = new Prize
        {
            CampaignId = campaign.Id,
            Name = "Test Prize",
            Description = "A test prize",
            Value = new Money(10.00m, "CAD"),
            TotalQuantity = 100,
            RemainingQuantity = 50,
            WinProbability = 0.1m,
            IsActive = true
        };
        context.Prizes.Add(prize);

        // Create staff
        var staff = new Staff
        {
            BusinessId = business.Id,
            Email = new Email($"staff{Guid.NewGuid()}@example.com"),
            PasswordHash = "hashedpassword",
            FirstName = "Test",
            LastName = "Staff",
            Role = StaffRole.Staff,
            IsActive = true
        };
        context.Staff.Add(staff);

        context.SaveChanges();
        return (player, business, campaign, prize, staff);
    }

    private PlayerPrize CreatePlayerPrize(ApplicationDbContext context, Player player, Prize prize, DateTime? expiresAt = null)
    {
        var playerPrize = new PlayerPrize
        {
            PlayerId = player.Id,
            PrizeId = prize.Id,
            RedemptionCode = RedemptionCode.Generate("TEST"),
            IsRedeemed = false,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        context.PlayerPrizes.Add(playerPrize);
        context.SaveChanges();
        return playerPrize;
    }

    [Property(MaxTest = 25)]
    public Property RedemptionWorkflowIntegrity()
    {
        return Prop.ForAll(
            GenerateRedemptionCodePrefix(),
            GenerateExpirationDays(),
            (codePrefix, expirationDays) =>
            {
                try
                {
                    // Arrange
                    using var context = CreateInMemoryContext();
                    var logger = new LoggerFactory().CreateLogger<RedemptionService>();
                    var analyticsService = new Mock<IAnalyticsService>().Object;
                    var redemptionService = new RedemptionService(context, analyticsService, logger);

                    var (player, business, campaign, prize, staff) = CreateTestData(context);
                    var expiresAt = DateTime.UtcNow.AddDays(Math.Max(1, expirationDays)); // Ensure future expiration
                    var playerPrize = CreatePlayerPrize(context, player, prize, expiresAt);

                    var redemptionCode = playerPrize.RedemptionCode.Value;

                    // Act 1: Verify redemption code
                    var verificationTask = redemptionService.VerifyRedemptionCodeAsync(redemptionCode);
                    verificationTask.Wait();
                    var verificationResult = verificationTask.Result;

                    // Assert 1: Verification should succeed for valid, non-expired, non-redeemed prize
                    var verificationValid = verificationResult.IsValid && 
                                          verificationResult.CanRedeem && 
                                          verificationResult.Prize != null &&
                                          verificationResult.Prize.RedemptionCode == redemptionCode;

                    if (!verificationValid)
                        return false;

                    // Act 2: Complete redemption
                    var completionTask = redemptionService.CompleteRedemptionAsync(redemptionCode, staff.Id);
                    completionTask.Wait();
                    var completionResult = completionTask.Result;

                    // Assert 2: Redemption should complete successfully
                    var completionValid = completionResult.Success && 
                                        completionResult.RedemptionId.HasValue &&
                                        completionResult.RedeemedAt.HasValue;

                    if (!completionValid)
                        return false;

                    // Act 3: Verify the prize is now redeemed
                    context.Entry(playerPrize).Reload();
                    var prizeRedeemed = playerPrize.IsRedeemed && 
                                      playerPrize.RedeemedAt.HasValue &&
                                      !playerPrize.CanBeRedeemed();

                    // Act 4: Try to redeem again (should fail)
                    var secondRedemptionTask = redemptionService.CompleteRedemptionAsync(redemptionCode, staff.Id);
                    secondRedemptionTask.Wait();
                    var secondRedemptionResult = secondRedemptionTask.Result;

                    var duplicateRedemptionPrevented = !secondRedemptionResult.Success &&
                                                     secondRedemptionResult.ErrorMessage != null &&
                                                     secondRedemptionResult.ErrorMessage.Contains("already been redeemed");

                    // Act 5: Verify code shows as already redeemed
                    var secondVerificationTask = redemptionService.VerifyRedemptionCodeAsync(redemptionCode);
                    secondVerificationTask.Wait();
                    var secondVerificationResult = secondVerificationTask.Result;

                    var verificationShowsRedeemed = secondVerificationResult.IsValid &&
                                                  !secondVerificationResult.CanRedeem &&
                                                  secondVerificationResult.ErrorMessage != null &&
                                                  secondVerificationResult.ErrorMessage.Contains("already been redeemed");

                    return prizeRedeemed && duplicateRedemptionPrevented && verificationShowsRedeemed;
                }
                catch (Exception)
                {
                    return false;
                }
            });
    }

    [Property(MaxTest = 25)]
    public Property PrizeExpirationManagement()
    {
        return Prop.ForAll(
            GenerateExpirationDays(),
            (expirationDays) =>
            {
                try
                {
                    // Arrange
                    using var context = CreateInMemoryContext();
                    var logger = new LoggerFactory().CreateLogger<Application.Services.PrizeExpirationService>();
                    var expirationService = new Application.Services.PrizeExpirationService(context, logger);

                    var (player, business, campaign, prize, staff) = CreateTestData(context);

                    // Create prizes with different expiration states
                    var expiredPrize = CreatePlayerPrize(context, player, prize, DateTime.UtcNow.AddDays(-1)); // Expired
                    var validPrize = CreatePlayerPrize(context, player, prize, DateTime.UtcNow.AddDays(7)); // Valid
                    var redeemedExpiredPrize = CreatePlayerPrize(context, player, prize, DateTime.UtcNow.AddDays(-1)); // Expired but redeemed
                    
                    // Mark one as redeemed
                    redeemedExpiredPrize.Redeem();
                    context.SaveChanges();

                    var initialPrizeCount = context.PlayerPrizes.Count();

                    // Act: Cleanup expired prizes
                    var cleanupTask = expirationService.CleanupExpiredPrizesAsync();
                    cleanupTask.Wait();
                    var cleanedCount = cleanupTask.Result;

                    // Assert: Only non-redeemed expired prizes should be cleaned up
                    var finalPrizeCount = context.PlayerPrizes.Count();
                    var expectedCleanedCount = 1; // Only the expired, non-redeemed prize
                    var expectedFinalCount = initialPrizeCount - expectedCleanedCount;

                    // Verify specific prizes
                    var expiredPrizeExists = context.PlayerPrizes.Any(pp => pp.Id == expiredPrize.Id);
                    var validPrizeExists = context.PlayerPrizes.Any(pp => pp.Id == validPrize.Id);
                    var redeemedExpiredPrizeExists = context.PlayerPrizes.Any(pp => pp.Id == redeemedExpiredPrize.Id);

                    return (cleanedCount == expectedCleanedCount) &&
                           (finalPrizeCount == expectedFinalCount) &&
                           (!expiredPrizeExists) && // Expired prize should be removed
                           (validPrizeExists) && // Valid prize should remain
                           (redeemedExpiredPrizeExists); // Redeemed expired prize should remain
                }
                catch (Exception)
                {
                    return false;
                }
            });
    }

    [Property(MaxTest = 25)]
    public Property InvalidRedemptionCodeHandling()
    {
        return Prop.ForAll(
            GenerateInvalidRedemptionCode(),
            (invalidCode) =>
            {
                try
                {
                    // Arrange
                    using var context = CreateInMemoryContext();
                    var logger = new LoggerFactory().CreateLogger<RedemptionService>();
                    var analyticsService = new Mock<IAnalyticsService>().Object;
                    var redemptionService = new RedemptionService(context, analyticsService, logger);

                    // Act: Try to verify invalid redemption code
                    var verificationTask = redemptionService.VerifyRedemptionCodeAsync(invalidCode);
                    verificationTask.Wait();
                    var verificationResult = verificationTask.Result;

                    // Assert: Should return invalid result
                    var verificationHandled = !verificationResult.IsValid &&
                                            !verificationResult.CanRedeem &&
                                            verificationResult.Prize == null &&
                                            verificationResult.ErrorMessage != null;

                    // Act: Try to complete redemption with invalid code
                    var completionTask = redemptionService.CompleteRedemptionAsync(invalidCode, Guid.NewGuid());
                    completionTask.Wait();
                    var completionResult = completionTask.Result;

                    // Assert: Should fail gracefully
                    var completionHandled = !completionResult.Success &&
                                          completionResult.RedemptionId == null &&
                                          completionResult.RedeemedAt == null &&
                                          completionResult.ErrorMessage != null;

                    return verificationHandled && completionHandled;
                }
                catch (Exception)
                {
                    return false;
                }
            });
    }

    [Property(MaxTest = 25)]
    public Property StaffAuthorizationEnforcement()
    {
        return Prop.ForAll(
            GenerateRedemptionCodePrefix(),
            (codePrefix) =>
            {
                try
                {
                    // Arrange
                    using var context = CreateInMemoryContext();
                    var logger = new LoggerFactory().CreateLogger<RedemptionService>();
                    var analyticsService = new Mock<IAnalyticsService>().Object;
                    var redemptionService = new RedemptionService(context, analyticsService, logger);

                    var (player, business, campaign, prize, staff) = CreateTestData(context);
                    var playerPrize = CreatePlayerPrize(context, player, prize);

                    // Create staff from different business
                    var otherBusiness = new Business
                    {
                        Name = "Other Business",
                        Email = new Email($"other{Guid.NewGuid()}@example.com"),
                        Address = "456 Other St",
                        IsActive = true
                    };
                    context.Businesses.Add(otherBusiness);

                    var unauthorizedStaff = new Staff
                    {
                        BusinessId = otherBusiness.Id,
                        Email = new Email($"unauthorized{Guid.NewGuid()}@example.com"),
                        PasswordHash = "hashedpassword",
                        FirstName = "Unauthorized",
                        LastName = "Staff",
                        Role = StaffRole.Staff,
                        IsActive = true
                    };
                    context.Staff.Add(unauthorizedStaff);
                    context.SaveChanges();

                    var redemptionCode = playerPrize.RedemptionCode.Value;

                    // Act 1: Try to redeem with authorized staff (should succeed)
                    var authorizedRedemptionTask = redemptionService.CompleteRedemptionAsync(redemptionCode, staff.Id);
                    authorizedRedemptionTask.Wait();
                    var authorizedResult = authorizedRedemptionTask.Result;

                    if (authorizedResult.Success)
                    {
                        // If authorized redemption succeeded, create a new prize for unauthorized test
                        var newPlayerPrize = CreatePlayerPrize(context, player, prize);
                        redemptionCode = newPlayerPrize.RedemptionCode.Value;
                    }

                    // Act 2: Try to redeem with unauthorized staff (should fail)
                    var unauthorizedRedemptionTask = redemptionService.CompleteRedemptionAsync(redemptionCode, unauthorizedStaff.Id);
                    unauthorizedRedemptionTask.Wait();
                    var unauthorizedResult = unauthorizedRedemptionTask.Result;

                    // Assert: Unauthorized staff should be rejected
                    var authorizationEnforced = !unauthorizedResult.Success &&
                                              unauthorizedResult.ErrorMessage != null &&
                                              unauthorizedResult.ErrorMessage.Contains("not authorized");

                    return authorizationEnforced;
                }
                catch (Exception)
                {
                    return false;
                }
            });
    }

    [Property(MaxTest = 25)]
    public Property ExpiredPrizeRedemptionPrevention()
    {
        return Prop.ForAll(
            GenerateRedemptionCodePrefix(),
            (codePrefix) =>
            {
                try
                {
                    // Arrange
                    using var context = CreateInMemoryContext();
                    var logger = new LoggerFactory().CreateLogger<RedemptionService>();
                    var analyticsService = new Mock<IAnalyticsService>().Object;
                    var redemptionService = new RedemptionService(context, analyticsService, logger);

                    var (player, business, campaign, prize, staff) = CreateTestData(context);
                    
                    // Create expired prize
                    var expiredPrize = CreatePlayerPrize(context, player, prize, DateTime.UtcNow.AddDays(-1));
                    var redemptionCode = expiredPrize.RedemptionCode.Value;

                    // Act 1: Verify expired prize
                    var verificationTask = redemptionService.VerifyRedemptionCodeAsync(redemptionCode);
                    verificationTask.Wait();
                    var verificationResult = verificationTask.Result;

                    // Assert 1: Should be valid but not redeemable
                    var verificationCorrect = verificationResult.IsValid &&
                                            !verificationResult.CanRedeem &&
                                            verificationResult.ErrorMessage != null &&
                                            verificationResult.ErrorMessage.Contains("expired");

                    // Act 2: Try to complete redemption of expired prize
                    var completionTask = redemptionService.CompleteRedemptionAsync(redemptionCode, staff.Id);
                    completionTask.Wait();
                    var completionResult = completionTask.Result;

                    // Assert 2: Should fail with expiration message
                    var completionPrevented = !completionResult.Success &&
                                            completionResult.ErrorMessage != null &&
                                            completionResult.ErrorMessage.Contains("expired");

                    // Assert 3: Prize should remain unredeemed
                    context.Entry(expiredPrize).Reload();
                    var prizeUnchanged = !expiredPrize.IsRedeemed && !expiredPrize.RedeemedAt.HasValue;

                    return verificationCorrect && completionPrevented && prizeUnchanged;
                }
                catch (Exception)
                {
                    return false;
                }
            });
    }

    private static Arbitrary<string> GenerateRedemptionCodePrefix()
    {
        return Arb.From(Gen.Elements("TEST", "PRIZE", "WIN", "REWARD", "BONUS"));
    }

    private static Arbitrary<int> GenerateExpirationDays()
    {
        return Arb.From(Gen.Choose(1, 30));
    }

    private static Arbitrary<string> GenerateInvalidRedemptionCode()
    {
        return Arb.From(Gen.OneOf(
            Gen.Constant(""), // Empty string
            Gen.Constant("12345"), // Too short
            Gen.Constant("A"), // Too short
            Gen.Constant("THIS-IS-A-VERY-LONG-REDEMPTION-CODE-THAT-EXCEEDS-THE-MAXIMUM-LENGTH"), // Too long
            Gen.Constant("NONEXISTENT123"), // Valid format but doesn't exist
            Gen.Constant("INVALID-CODE-456") // Valid format but doesn't exist
        ));
    }
}