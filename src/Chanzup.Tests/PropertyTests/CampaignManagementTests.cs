using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Chanzup.Application.DTOs;
using Chanzup.Application.Services;
using Chanzup.Domain.Entities;
using Chanzup.Domain.ValueObjects;
using Chanzup.Infrastructure.Data;
using Chanzup.Infrastructure.Services;
using Xunit;

namespace Chanzup.Tests.PropertyTests;

/// <summary>
/// Feature: vancouver-rewards-platform, Property 2: QR Code Uniqueness
/// Feature: vancouver-rewards-platform, Property 5: Campaign Lifecycle Management
/// Validates: Requirements 1.2, 2.1, 2.5, 2.6
/// </summary>
public class CampaignManagementTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var tenantContext = new TenantContext();
        return new ApplicationDbContext(options, tenantContext);
    }

    private CampaignService CreateCampaignService(ApplicationDbContext context)
    {
        var qrCodeService = new QRCodeService();
        return new CampaignService(context, qrCodeService);
    }

    [Property(MaxTest = 25)]
    public Property QRCodeUniquenessProperty()
    {
        return Prop.ForAll(
            GenerateMultipleCampaigns(),
            (campaigns) =>
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var qrCodeService = new QRCodeService();

                // Create test campaigns with different IDs
                var campaignList = campaigns.ToList();
                var campaignIds = campaignList.Select(_ => Guid.NewGuid()).ToList();
                var generatedQRCodes = new List<string>();

                // Act - Generate QR codes for different campaign IDs
                foreach (var campaignId in campaignIds)
                {
                    var qrCode = qrCodeService.GenerateQRCode(campaignId);
                    generatedQRCodes.Add(qrCode);
                }

                // Assert - All QR codes should be unique
                var uniqueQRCodes = generatedQRCodes.Distinct().ToList();
                return generatedQRCodes.Count == uniqueQRCodes.Count;
            });
    }

    [Property(MaxTest = 25)]
    public Property CampaignLifecycleManagementProperty()
    {
        return Prop.ForAll(
            GenerateValidCampaignRequest(),
            (campaignRequest) =>
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var campaignService = CreateCampaignService(context);

                // Create a test business
                var business = new Business
                {
                    Name = "Test Business",
                    Email = new Email("test@example.com"),
                    Location = new Location(49.2827m, -123.1207m)
                };
                context.Businesses.Add(business);
                context.SaveChanges();

                try
                {
                    // Act & Assert - Test campaign lifecycle synchronously
                    // 1. Create campaign - should be active by default
                    var createdCampaign = campaignService.CreateCampaignAsync(business.Id, campaignRequest).Result;
                    if (createdCampaign == null) return false;

                    var initiallyActive = createdCampaign.IsActive;

                    // 2. Deactivate campaign
                    var deactivated = campaignService.DeactivateCampaignAsync(createdCampaign.Id, business.Id).Result;
                    if (!deactivated) return false;

                    var deactivatedCampaign = campaignService.GetCampaignAsync(createdCampaign.Id, business.Id).Result;
                    if (deactivatedCampaign == null) return false;

                    var isDeactivated = !deactivatedCampaign.IsActive;

                    // 3. Reactivate campaign
                    var reactivated = campaignService.ActivateCampaignAsync(createdCampaign.Id, business.Id).Result;
                    if (!reactivated) return false;

                    var reactivatedCampaign = campaignService.GetCampaignAsync(createdCampaign.Id, business.Id).Result;
                    if (reactivatedCampaign == null) return false;

                    var isReactivated = reactivatedCampaign.IsActive;

                    // 4. Update campaign
                    var updateRequest = new UpdateCampaignRequest
                    {
                        Name = "Updated " + campaignRequest.Name,
                        TokenCostPerSpin = campaignRequest.TokenCostPerSpin + 1
                    };

                    var updatedCampaign = campaignService.UpdateCampaignAsync(createdCampaign.Id, business.Id, updateRequest).Result;
                    if (updatedCampaign == null) return false;

                    var nameUpdated = updatedCampaign.Name == updateRequest.Name;
                    var costUpdated = updatedCampaign.TokenCostPerSpin == updateRequest.TokenCostPerSpin;

                    // 5. Delete campaign
                    var deleted = campaignService.DeleteCampaignAsync(createdCampaign.Id, business.Id).Result;
                    if (!deleted) return false;

                    var deletedCampaign = campaignService.GetCampaignAsync(createdCampaign.Id, business.Id).Result;
                    var isDeleted = deletedCampaign == null;

                    // Verify all lifecycle operations worked correctly
                    return initiallyActive &&
                           isDeactivated &&
                           isReactivated &&
                           nameUpdated &&
                           costUpdated &&
                           isDeleted;
                }
                catch
                {
                    // If any operation fails, the lifecycle is not properly managed
                    return false;
                }
            });
    }

    [Property(MaxTest = 25)]
    public Property CampaignVisibilityProperty()
    {
        return Prop.ForAll(
            GenerateLocationAndCampaigns(),
            (data) =>
            {
                var (searchLat, searchLng, campaigns) = data;
                var campaignList = campaigns.ToList();

                // Arrange
                using var context = CreateInMemoryContext();
                var campaignService = CreateCampaignService(context);

                var createdCampaigns = new List<CampaignResponse>();

                // Create businesses and campaigns at various locations
                foreach (var (businessLat, businessLng, campaignRequest) in campaignList)
                {
                    try
                    {
                        var business = new Business
                        {
                            Name = $"Business at {businessLat},{businessLng}",
                            Email = new Email($"business{Guid.NewGuid():N}@example.com"),
                            Location = new Location(businessLat, businessLng)
                        };
                        context.Businesses.Add(business);
                        context.SaveChanges();

                        var campaign = campaignService.CreateCampaignAsync(business.Id, campaignRequest).Result;
                        createdCampaigns.Add(campaign);
                    }
                    catch
                    {
                        // Skip invalid campaigns
                        continue;
                    }
                }

                if (createdCampaigns.Count == 0) return true; // No campaigns to test

                // Act - Search for nearby campaigns
                var nearbyCampaigns = campaignService.GetActiveCampaignsNearLocationAsync(
                    (double)searchLat, (double)searchLng, 10.0).Result; // 10km radius

                // Assert - All returned campaigns should be within the search radius
                var searchLocation = new Location(searchLat, searchLng);
                
                foreach (var campaign in nearbyCampaigns)
                {
                    if (campaign.Business?.Latitude == null || campaign.Business?.Longitude == null)
                        continue;

                    var businessLocation = new Location(campaign.Business.Latitude.Value, campaign.Business.Longitude.Value);
                    var distance = businessLocation.DistanceTo(searchLocation);
                    
                    // Distance should be within 10km (10000 meters)
                    if (distance > 10000)
                        return false;
                }

                return true;
            });
    }

    private static Arbitrary<Microsoft.FSharp.Collections.FSharpList<CreateCampaignRequest>> GenerateMultipleCampaigns()
    {
        return Arb.From(
            Gen.ListOf(GenerateValidCampaignRequest().Generator)
               .Where(list => list.Count() >= 2 && list.Count() <= 10));
    }

    private static Arbitrary<CreateCampaignRequest> GenerateValidCampaignRequest()
    {
        return Arb.From(
            from name in Arb.Generate<NonEmptyString>()
            from description in Arb.Generate<string>()
            from gameType in Gen.Elements(GameType.WheelOfLuck, GameType.TreasureHunt)
            from tokenCost in Gen.Choose(1, 10)
            from maxSpins in Gen.Choose(1, 20)
            from startDate in GenerateFutureDate()
            from endDate in GenerateFutureDate()
            from prizes in GenerateValidPrizes()
            select new CreateCampaignRequest
            {
                Name = name.Get,
                Description = description,
                GameType = gameType,
                TokenCostPerSpin = tokenCost,
                MaxSpinsPerDay = maxSpins,
                StartDate = startDate,
                EndDate = endDate > startDate ? endDate : startDate.AddDays(7),
                Prizes = prizes.ToList()
            });
    }

    private static Arbitrary<(decimal, decimal, Microsoft.FSharp.Collections.FSharpList<(decimal, decimal, CreateCampaignRequest)>)> GenerateLocationAndCampaigns()
    {
        return Arb.From(
            from searchLat in Gen.Choose(-90, 90).Select(x => (decimal)x)
            from searchLng in Gen.Choose(-180, 180).Select(x => (decimal)x)
            from campaigns in Gen.ListOf(
                from businessLat in Gen.Choose(-90, 90).Select(x => (decimal)x)
                from businessLng in Gen.Choose(-180, 180).Select(x => (decimal)x)
                from campaign in GenerateValidCampaignRequest().Generator
                select (businessLat, businessLng, campaign))
                .Where(list => list.Count() >= 1 && list.Count() <= 5)
            select (searchLat, searchLng, campaigns));
    }

    private static Gen<DateTime> GenerateFutureDate()
    {
        return Gen.Choose(0, 365)
                  .Select(days => DateTime.UtcNow.AddDays(days));
    }

    private static Gen<Microsoft.FSharp.Collections.FSharpList<CreatePrizeRequest>> GenerateValidPrizes()
    {
        return Gen.ListOf(
            from name in Arb.Generate<NonEmptyString>()
            from description in Arb.Generate<string>()
            from value in Gen.Choose(0, 100).Select(x => (decimal?)x)
            from quantity in Gen.Choose(1, 100)
            from probability in Gen.Choose(1, 100).Select(x => (decimal)x / 100)
            select new CreatePrizeRequest
            {
                Name = name.Get,
                Description = description,
                Value = value,
                TotalQuantity = quantity,
                WinProbability = probability
            })
            .Where(list => list.Count() >= 1 && list.Count() <= 5)
            .Where(list => list.Sum(p => p.WinProbability) <= 1.0m); // Ensure total probability <= 1.0
    }
}