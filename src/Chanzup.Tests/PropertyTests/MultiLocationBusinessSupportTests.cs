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
/// Feature: vancouver-rewards-platform, Property 19: Multi-Location Business Support
/// Validates: Requirements 11.1, 11.2, 11.3, 11.5
/// </summary>
public class MultiLocationBusinessSupportTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var tenantContext = new TenantContext();
        return new ApplicationDbContext(options, tenantContext);
    }

    [Property(MaxTest = 25)]
    public Property MultiLocationCampaignTargetingProperty()
    {
        return Prop.ForAll(
            GenerateBusinessWithMultipleLocations(),
            (businessData) =>
            {
                var (businessName, locations) = businessData;
                var locationList = locations.ToList();
                
                if (locationList.Count < 2) return true; // Need at least 2 locations for meaningful test

                // Arrange
                using var context = CreateInMemoryContext();
                var campaignService = CreateCampaignService(context);
                var analyticsService = new AnalyticsService(context);

                try
                {
                    // Create business with multiple locations
                    var business = new Business
                    {
                        Name = businessName,
                        Email = new Email($"business{Guid.NewGuid():N}@example.com"),
                        Location = new Location(locationList[0].Latitude, locationList[0].Longitude)
                    };
                    context.Businesses.Add(business);
                    context.SaveChanges();

                    // Add business locations
                    var businessLocations = new List<BusinessLocation>();
                    foreach (var location in locationList)
                    {
                        var businessLocation = new BusinessLocation
                        {
                            BusinessId = business.Id,
                            Name = location.Name,
                            Address = location.Address,
                            Location = new Location(location.Latitude, location.Longitude),
                            QRCode = Guid.NewGuid().ToString()
                        };
                        context.BusinessLocations.Add(businessLocation);
                        businessLocations.Add(businessLocation);
                    }
                    context.SaveChanges();

                    // Test 1: Campaign targeting all locations
                    var allLocationsCampaign = campaignService.CreateCampaignAsync(business.Id, new CreateCampaignRequest
                    {
                        Name = "All Locations Campaign",
                        Description = "Campaign for all locations",
                        GameType = GameType.WheelOfLuck,
                        TokenCostPerSpin = 5,
                        MaxSpinsPerDay = 3,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddDays(30),
                        Prizes = new List<CreatePrizeRequest>
                        {
                            new() { Name = "Test Prize", TotalQuantity = 100, WinProbability = 0.1m }
                        }
                    }).Result;

                    if (allLocationsCampaign == null) return false;

                    // Verify campaign is available at all locations
                    var campaign = context.Campaigns
                        .Include(c => c.CampaignLocations)
                        .First(c => c.Id == allLocationsCampaign.Id);

                    var availableAtAllLocations = businessLocations.All(bl => 
                        campaign.IsAvailableAtLocation(bl.Id));

                    if (!availableAtAllLocations) return false;

                    // Test 2: Campaign targeting specific locations
                    var targetLocations = businessLocations.Take(2).ToList();
                    var specificLocationsCampaign = campaignService.CreateCampaignAsync(business.Id, new CreateCampaignRequest
                    {
                        Name = "Specific Locations Campaign",
                        Description = "Campaign for specific locations",
                        GameType = GameType.WheelOfLuck,
                        TokenCostPerSpin = 5,
                        MaxSpinsPerDay = 3,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddDays(30),
                        Prizes = new List<CreatePrizeRequest>
                        {
                            new() { Name = "Test Prize", TotalQuantity = 50, WinProbability = 0.1m }
                        }
                    }).Result;

                    if (specificLocationsCampaign == null) return false;

                    // Set specific location targeting
                    var specificCampaign = context.Campaigns
                        .Include(c => c.CampaignLocations)
                        .First(c => c.Id == specificLocationsCampaign.Id);

                    specificCampaign.SetLocationTargeting(targetLocations.Select(tl => tl.Id));
                    context.SaveChanges();

                    // Verify campaign is only available at target locations
                    var availableAtTargetLocations = targetLocations.All(tl => 
                        specificCampaign.IsAvailableAtLocation(tl.Id));

                    var notAvailableAtOtherLocations = businessLocations
                        .Except(targetLocations)
                        .All(ol => !specificCampaign.IsAvailableAtLocation(ol.Id));

                    if (!availableAtTargetLocations || !notAvailableAtOtherLocations) return false;

                    // Test 3: Multi-location analytics consolidation
                    var multiLocationMetrics = analyticsService.GetMultiLocationBusinessMetricsAsync(
                        business.Id, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1)).Result;

                    if (multiLocationMetrics == null) return false;

                    // Verify consolidated metrics include all locations
                    var allLocationsIncluded = multiLocationMetrics.TotalLocations == businessLocations.Count;
                    var consolidatedMetricsExist = multiLocationMetrics.ConsolidatedMetrics != null;
                    var locationMetricsExist = multiLocationMetrics.LocationMetrics.Count <= businessLocations.Count;

                    return allLocationsIncluded && consolidatedMetricsExist && locationMetricsExist;
                }
                catch
                {
                    return false;
                }
            });
    }

    [Property(MaxTest = 25)]
    public Property LocationSpecificInventoryProperty()
    {
        return Prop.ForAll(
            GenerateBusinessWithLocationsAndPrizes(),
            (data) =>
            {
                var (businessName, locations, prizeData) = data;
                var locationList = locations.ToList();
                var (prizeName, totalQuantity) = prizeData;
                
                if (locationList.Count < 2 || totalQuantity < locationList.Count) return true;

                // Arrange
                using var context = CreateInMemoryContext();
                var campaignService = CreateCampaignService(context);
                var inventoryService = new LocationInventoryService(context);

                try
                {
                    // Create business with multiple locations
                    var business = new Business
                    {
                        Name = businessName,
                        Email = new Email($"business{Guid.NewGuid():N}@example.com"),
                        Location = new Location(locationList[0].Latitude, locationList[0].Longitude)
                    };
                    context.Businesses.Add(business);
                    context.SaveChanges();

                    // Add business locations
                    var businessLocations = new List<BusinessLocation>();
                    foreach (var location in locationList)
                    {
                        var businessLocation = new BusinessLocation
                        {
                            BusinessId = business.Id,
                            Name = location.Name,
                            Address = location.Address,
                            Location = new Location(location.Latitude, location.Longitude),
                            QRCode = Guid.NewGuid().ToString()
                        };
                        context.BusinessLocations.Add(businessLocation);
                        businessLocations.Add(businessLocation);
                    }
                    context.SaveChanges();

                    // Create campaign with prize
                    var campaign = campaignService.CreateCampaignAsync(business.Id, new CreateCampaignRequest
                    {
                        Name = "Multi-Location Campaign",
                        Description = "Campaign with location-specific inventory",
                        GameType = GameType.WheelOfLuck,
                        TokenCostPerSpin = 5,
                        MaxSpinsPerDay = 3,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddDays(30),
                        Prizes = new List<CreatePrizeRequest>
                        {
                            new() { Name = prizeName, TotalQuantity = totalQuantity, WinProbability = 0.1m }
                        }
                    }).Result;

                    if (campaign == null) return false;

                    var prize = context.Prizes.First(p => p.CampaignId == campaign.Id);

                    // Test 1: Setup location-specific inventory
                    var quantityPerLocation = totalQuantity / locationList.Count;
                    var locationQuantities = businessLocations.ToDictionary(
                        bl => bl.Id, 
                        bl => quantityPerLocation);

                    inventoryService.SetupLocationSpecificInventoryAsync(prize.Id, locationQuantities).Wait();

                    // Verify inventory was set up correctly
                    var inventorySetupCorrectly = businessLocations.All(bl =>
                        inventoryService.GetRemainingQuantityAtLocationAsync(prize.Id, bl.Id).Result == quantityPerLocation);

                    if (!inventorySetupCorrectly) return false;

                    // Test 2: Reserve prizes at specific locations
                    var firstLocation = businessLocations.First();
                    var initialQuantity = inventoryService.GetRemainingQuantityAtLocationAsync(prize.Id, firstLocation.Id).Result;
                    
                    inventoryService.ReservePrizeAtLocationAsync(prize.Id, firstLocation.Id).Wait();
                    
                    var quantityAfterReservation = inventoryService.GetRemainingQuantityAtLocationAsync(prize.Id, firstLocation.Id).Result;
                    var reservationWorked = quantityAfterReservation == initialQuantity - 1;

                    if (!reservationWorked) return false;

                    // Test 3: Verify other locations are unaffected
                    var otherLocationsUnaffected = businessLocations
                        .Skip(1)
                        .All(bl => inventoryService.GetRemainingQuantityAtLocationAsync(prize.Id, bl.Id).Result == quantityPerLocation);

                    if (!otherLocationsUnaffected) return false;

                    // Test 4: Transfer inventory between locations
                    if (businessLocations.Count >= 2)
                    {
                        var fromLocation = businessLocations[0];
                        var toLocation = businessLocations[1];
                        var transferQuantity = Math.Min(2, quantityAfterReservation);

                        if (transferQuantity > 0)
                        {
                            var fromQuantityBefore = inventoryService.GetRemainingQuantityAtLocationAsync(prize.Id, fromLocation.Id).Result;
                            var toQuantityBefore = inventoryService.GetRemainingQuantityAtLocationAsync(prize.Id, toLocation.Id).Result;

                            inventoryService.TransferInventoryBetweenLocationsAsync(
                                prize.Id, fromLocation.Id, toLocation.Id, transferQuantity).Wait();

                            var fromQuantityAfter = inventoryService.GetRemainingQuantityAtLocationAsync(prize.Id, fromLocation.Id).Result;
                            var toQuantityAfter = inventoryService.GetRemainingQuantityAtLocationAsync(prize.Id, toLocation.Id).Result;

                            var transferWorked = 
                                fromQuantityAfter == fromQuantityBefore - transferQuantity &&
                                toQuantityAfter == toQuantityBefore + transferQuantity;

                            if (!transferWorked) return false;
                        }
                    }

                    return true;
                }
                catch
                {
                    return false;
                }
            });
    }

    [Property(MaxTest = 25)]
    public Property StaffLocationAccessProperty()
    {
        return Prop.ForAll(
            GenerateBusinessWithLocationsAndStaff(),
            (data) =>
            {
                var (businessName, locations, staffData) = data;
                var locationList = locations.ToList();
                var (staffName, staffRole) = staffData;
                
                if (locationList.Count < 2) return true;

                // Arrange
                using var context = CreateInMemoryContext();
                var staffAccessService = new StaffLocationAccessService(context);

                try
                {
                    // Create business with multiple locations
                    var business = new Business
                    {
                        Name = businessName,
                        Email = new Email($"business{Guid.NewGuid():N}@example.com"),
                        Location = new Location(locationList[0].Latitude, locationList[0].Longitude)
                    };
                    context.Businesses.Add(business);
                    context.SaveChanges();

                    // Add business locations
                    var businessLocations = new List<BusinessLocation>();
                    foreach (var location in locationList)
                    {
                        var businessLocation = new BusinessLocation
                        {
                            BusinessId = business.Id,
                            Name = location.Name,
                            Address = location.Address,
                            Location = new Location(location.Latitude, location.Longitude),
                            QRCode = Guid.NewGuid().ToString()
                        };
                        context.BusinessLocations.Add(businessLocation);
                        businessLocations.Add(businessLocation);
                    }
                    context.SaveChanges();

                    // Create staff member
                    var staff = new Staff
                    {
                        BusinessId = business.Id,
                        Email = new Email($"staff{Guid.NewGuid():N}@example.com"),
                        PasswordHash = "hashedpassword",
                        FirstName = staffName,
                        LastName = "Test",
                        Role = staffRole
                    };
                    context.Staff.Add(staff);
                    context.SaveChanges();

                    // Test 1: Grant access to specific locations
                    var targetLocations = businessLocations.Take(2).ToList();
                    foreach (var location in targetLocations)
                    {
                        staffAccessService.GrantLocationAccessAsync(staff.Id, location.Id, LocationPermissions.All).Wait();
                    }

                    // Verify staff has access to target locations
                    var hasAccessToTargetLocations = targetLocations.All(tl =>
                        staffAccessService.HasAccessToLocationAsync(staff.Id, tl.Id).Result);

                    if (!hasAccessToTargetLocations) return false;

                    // Verify staff doesn't have access to other locations
                    var otherLocations = businessLocations.Except(targetLocations).ToList();
                    var noAccessToOtherLocations = otherLocations.All(ol =>
                        !staffAccessService.HasAccessToLocationAsync(staff.Id, ol.Id).Result);

                    if (!noAccessToOtherLocations) return false;

                    // Test 2: Grant all locations access
                    staffAccessService.SetAllLocationsAccessAsync(staff.Id).Wait();

                    // Verify staff now has access to all locations
                    var hasAccessToAllLocations = businessLocations.All(bl =>
                        staffAccessService.HasAccessToLocationAsync(staff.Id, bl.Id).Result);

                    if (!hasAccessToAllLocations) return false;

                    // Test 3: Revoke access to specific location
                    var locationToRevoke = businessLocations.First();
                    staffAccessService.RevokeLocationAccessAsync(staff.Id, locationToRevoke.Id).Wait();

                    // Since staff had all locations access, revoking one location should not affect access
                    // (all locations access overrides specific location access)
                    var stillHasAccessAfterRevoke = staffAccessService.HasAccessToLocationAsync(staff.Id, locationToRevoke.Id).Result;

                    // Test 4: Permission-based access
                    var testLocation = businessLocations.Last();
                    var hasManageCampaignsPermission = staffAccessService.HasPermissionAtLocationAsync(
                        staff.Id, testLocation.Id, LocationPermissions.ManageCampaigns).Result;

                    var hasVerifyRedemptionsPermission = staffAccessService.HasPermissionAtLocationAsync(
                        staff.Id, testLocation.Id, LocationPermissions.VerifyRedemptions).Result;

                    // Staff with all locations access should have all permissions
                    var hasAllPermissions = hasManageCampaignsPermission && hasVerifyRedemptionsPermission;

                    return stillHasAccessAfterRevoke && hasAllPermissions;
                }
                catch
                {
                    return false;
                }
            });
    }

    private CampaignService CreateCampaignService(ApplicationDbContext context)
    {
        var qrCodeService = new QRCodeService();
        return new CampaignService(context, qrCodeService);
    }

    private static Arbitrary<(string, Microsoft.FSharp.Collections.FSharpList<LocationData>)> GenerateBusinessWithMultipleLocations()
    {
        return Arb.From(
            from businessName in Arb.Generate<NonEmptyString>()
            from locations in Gen.ListOf(GenerateLocationData())
                .Where(list => list.Count() >= 2 && list.Count() <= 5)
            select (businessName.Get, locations));
    }

    private static Arbitrary<(string, Microsoft.FSharp.Collections.FSharpList<LocationData>, (string, int))> GenerateBusinessWithLocationsAndPrizes()
    {
        return Arb.From(
            from businessName in Arb.Generate<NonEmptyString>()
            from locations in Gen.ListOf(GenerateLocationData())
                .Where(list => list.Count() >= 2 && list.Count() <= 4)
            from prizeName in Arb.Generate<NonEmptyString>()
            from totalQuantity in Gen.Choose(10, 100)
            select (businessName.Get, locations, (prizeName.Get, totalQuantity)));
    }

    private static Arbitrary<(string, Microsoft.FSharp.Collections.FSharpList<LocationData>, (string, StaffRole))> GenerateBusinessWithLocationsAndStaff()
    {
        return Arb.From(
            from businessName in Arb.Generate<NonEmptyString>()
            from locations in Gen.ListOf(GenerateLocationData())
                .Where(list => list.Count() >= 2 && list.Count() <= 4)
            from staffName in Arb.Generate<NonEmptyString>()
            from staffRole in Gen.Elements(StaffRole.Staff, StaffRole.Manager, StaffRole.Owner)
            select (businessName.Get, locations, (staffName.Get, staffRole)));
    }

    private static Gen<LocationData> GenerateLocationData()
    {
        return from name in Arb.Generate<NonEmptyString>()
               from address in Arb.Generate<NonEmptyString>()
               from lat in Gen.Choose(-90, 90).Select(x => (decimal)x)
               from lng in Gen.Choose(-180, 180).Select(x => (decimal)x)
               select new LocationData
               {
                   Name = name.Get,
                   Address = address.Get,
                   Latitude = lat,
                   Longitude = lng
               };
    }

    private class LocationData
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
}