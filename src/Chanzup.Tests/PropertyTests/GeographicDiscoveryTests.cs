using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
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
/// Feature: vancouver-rewards-platform, Property 18: Geographic Discovery Accuracy
/// Validates: Requirements 9.1, 9.5
/// </summary>
public class GeographicDiscoveryTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var tenantContext = new TenantContext();
        return new ApplicationDbContext(options, tenantContext);
    }

    private DiscoveryService CreateDiscoveryService(ApplicationDbContext context)
    {
        return new DiscoveryService(context);
    }

    private MappingService CreateMappingService(ApplicationDbContext context)
    {
        return new MappingService(context);
    }

    [Property(MaxTest = 25)]
    public Property GeographicDiscoveryAccuracyProperty()
    {
        return Prop.ForAll(
            GenerateGeographicDiscoveryScenario(),
            (scenario) =>
            {
                var (searchLat, searchLng, radiusKm, businesses) = scenario;

                // Arrange
                using var context = CreateInMemoryContext();
                var discoveryService = CreateDiscoveryService(context);

                // Add businesses to context
                foreach (var business in businesses)
                {
                    context.Businesses.Add(business);
                }
                context.SaveChanges();

                try
                {
                    // Act - Search for nearby businesses
                    var result = discoveryService.GetNearbyBusinessesAsync(
                        searchLat, searchLng, radiusKm, activeOnly: false).Result;

                    // Assert - All returned businesses should be within the specified radius
                    var searchLocation = new Location((decimal)searchLat, (decimal)searchLng);
                    var radiusMeters = radiusKm * 1000;

                    foreach (var discoveredBusiness in result.Businesses)
                    {
                        var businessLocation = new Location(
                            discoveredBusiness.Location.Latitude, 
                            discoveredBusiness.Location.Longitude);
                        
                        var actualDistance = searchLocation.DistanceTo(businessLocation);
                        
                        // Business should be within the search radius (with small tolerance for rounding)
                        if (actualDistance > radiusMeters + 10) // 10m tolerance
                        {
                            return false;
                        }

                        // Distance reported in response should be accurate
                        var reportedDistanceMeters = discoveredBusiness.DistanceKm * 1000;
                        var distanceDifference = Math.Abs(actualDistance - reportedDistanceMeters);
                        
                        // Allow 1% tolerance for distance calculation differences
                        if (distanceDifference > actualDistance * 0.01 + 1)
                        {
                            return false;
                        }
                    }

                    // All businesses within radius should be included (if active)
                    var expectedBusinesses = businesses.Where(b => 
                    {
                        if (b.Location == null || !b.IsActive) return false;
                        var distance = searchLocation.DistanceTo(b.Location);
                        return distance <= radiusMeters;
                    }).ToList();

                    // The number of returned businesses should match expected
                    if (result.Businesses.Count != expectedBusinesses.Count)
                    {
                        return false;
                    }

                    // Results should be ordered by distance (closest first)
                    for (int i = 1; i < result.Businesses.Count; i++)
                    {
                        if (result.Businesses[i].DistanceKm < result.Businesses[i - 1].DistanceKm)
                        {
                            return false; // Not properly ordered by distance
                        }
                    }

                    return true;
                }
                catch (ArgumentException)
                {
                    // Invalid input parameters should throw ArgumentException
                    return searchLat < -90 || searchLat > 90 || 
                           searchLng < -180 || searchLng > 180 || 
                           radiusKm <= 0 || radiusKm > 100;
                }
                catch
                {
                    // Other exceptions indicate system failure
                    return false;
                }
            });
    }

    [Property(MaxTest = 25)]
    public Property DistanceCalculationAccuracyProperty()
    {
        return Prop.ForAll(
            GenerateDistanceCalculationScenario(),
            (scenario) =>
            {
                var (lat1, lng1, lat2, lng2) = scenario;

                // Arrange
                using var context = CreateInMemoryContext();
                var mappingService = CreateMappingService(context);

                try
                {
                    // Act - Calculate distance using the mapping service
                    var calculatedDistance = mappingService.CalculateDistanceAsync(lat1, lng1, lat2, lng2).Result;

                    // Assert - Distance should be non-negative
                    if (calculatedDistance < 0)
                    {
                        return false;
                    }

                    // Distance from a point to itself should be zero (or very close)
                    if (Math.Abs(lat1 - lat2) < 0.0001 && Math.Abs(lng1 - lng2) < 0.0001)
                    {
                        return calculatedDistance < 0.001; // Less than 1 meter
                    }

                    // Distance should be symmetric (A to B = B to A)
                    var reverseDistance = mappingService.CalculateDistanceAsync(lat2, lng2, lat1, lng1).Result;
                    var distanceDifference = Math.Abs(calculatedDistance - reverseDistance);
                    
                    // Allow small tolerance for floating point precision
                    if (distanceDifference > 0.001) // 1 meter tolerance
                    {
                        return false;
                    }

                    // Verify using Location value object calculation
                    var location1 = new Location((decimal)lat1, (decimal)lng1);
                    var location2 = new Location((decimal)lat2, (decimal)lng2);
                    var expectedDistanceMeters = location1.DistanceTo(location2);
                    var expectedDistanceKm = expectedDistanceMeters / 1000.0;

                    var calculationDifference = Math.Abs(calculatedDistance - expectedDistanceKm);
                    
                    // Allow 1% tolerance for different calculation methods
                    return calculationDifference <= Math.Max(expectedDistanceKm * 0.01, 0.001);
                }
                catch (ArgumentException)
                {
                    // Invalid coordinates should throw ArgumentException
                    return lat1 < -90 || lat1 > 90 || lng1 < -180 || lng1 > 180 ||
                           lat2 < -90 || lat2 > 90 || lng2 < -180 || lng2 > 180;
                }
                catch
                {
                    // Other exceptions indicate system failure
                    return false;
                }
            });
    }

    [Property(MaxTest = 25)]
    public Property BusinessFilteringAccuracyProperty()
    {
        return Prop.ForAll(
            GenerateBusinessFilteringScenario(),
            (scenario) =>
            {
                var (searchLat, searchLng, radiusKm, businesses, activeOnly) = scenario;

                // Arrange
                using var context = CreateInMemoryContext();
                var discoveryService = CreateDiscoveryService(context);

                // Add businesses with campaigns to context
                foreach (var (business, campaigns) in businesses)
                {
                    context.Businesses.Add(business);
                    foreach (var campaign in campaigns)
                    {
                        context.Campaigns.Add(campaign);
                    }
                }
                context.SaveChanges();

                try
                {
                    // Act - Search with filtering
                    var result = discoveryService.GetNearbyBusinessesAsync(
                        searchLat, searchLng, radiusKm, activeOnly: activeOnly).Result;

                    // Assert - Filtering should be applied correctly
                    foreach (var discoveredBusiness in result.Businesses)
                    {
                        var originalBusiness = businesses.First(b => b.Item1.Id == discoveredBusiness.BusinessId).Item1;
                        var businessCampaigns = businesses.First(b => b.Item1.Id == discoveredBusiness.BusinessId).Item2;

                        // Business should be active
                        if (!originalBusiness.IsActive)
                        {
                            return false;
                        }

                        // If activeOnly is true, business should have active campaigns
                        if (activeOnly)
                        {
                            var now = DateTime.UtcNow;
                            var hasActiveCampaigns = businessCampaigns.Any(c => 
                                c.IsActive && 
                                c.StartDate <= now && 
                                (c.EndDate == null || c.EndDate >= now));

                            if (!hasActiveCampaigns)
                            {
                                return false;
                            }
                        }

                        // Campaign count should be accurate
                        var now2 = DateTime.UtcNow;
                        var expectedActiveCampaigns = businessCampaigns.Count(c => 
                            c.IsActive && 
                            c.StartDate <= now2 && 
                            (c.EndDate == null || c.EndDate >= now2));

                        if (discoveredBusiness.ActiveCampaigns != expectedActiveCampaigns)
                        {
                            return false;
                        }
                    }

                    return true;
                }
                catch (ArgumentException)
                {
                    // Invalid input should throw ArgumentException
                    return searchLat < -90 || searchLat > 90 || 
                           searchLng < -180 || searchLng > 180 || 
                           radiusKm <= 0 || radiusKm > 100;
                }
                catch
                {
                    // Other exceptions indicate system failure
                    return false;
                }
            });
    }

    [Property(MaxTest = 25)]
    public Property RadiusLimitEnforcementProperty()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(-200, 200).Select(x => (double)x)),
            (radiusKm) =>
            {
                // Fixed search location
                const double searchLat = 49.2827;
                const double searchLng = -123.1207;

                // Arrange
                using var context = CreateInMemoryContext();
                var discoveryService = CreateDiscoveryService(context);

                try
                {
                    // Act - Attempt search with various radius values
                    var result = discoveryService.GetNearbyBusinessesAsync(
                        searchLat, searchLng, radiusKm).Result;

                    // Assert - Should only succeed for valid radius values
                    return radiusKm > 0 && radiusKm <= 100;
                }
                catch (ArgumentException)
                {
                    // Should throw ArgumentException for invalid radius
                    return radiusKm <= 0 || radiusKm > 100;
                }
                catch
                {
                    // Other exceptions indicate system failure
                    return false;
                }
            });
    }

    private static Arbitrary<(double, double, double, List<Business>)> GenerateGeographicDiscoveryScenario()
    {
        return Arb.From(
            from searchLat in Gen.Choose(-89, 89).Select(x => (double)x)
            from searchLng in Gen.Choose(-179, 179).Select(x => (double)x)
            from radiusKm in Gen.Choose(1, 50).Select(x => (double)x)
            from businessCount in Gen.Choose(0, 10)
            select CreateGeographicDiscoveryScenario(searchLat, searchLng, radiusKm, businessCount));
    }

    private static (double, double, double, List<Business>) CreateGeographicDiscoveryScenario(
        double searchLat, double searchLng, double radiusKm, int businessCount)
    {
        var businesses = new List<Business>();
        var random = new System.Random();

        for (int i = 0; i < businessCount; i++)
        {
            // Create businesses both inside and outside the search radius
            var isInside = random.NextDouble() > 0.3; // 70% chance of being inside
            
            double businessLat, businessLng;
            
            if (isInside)
            {
                // Place business within radius
                var angle = random.NextDouble() * 2 * Math.PI;
                var distance = random.NextDouble() * radiusKm * 0.9; // 90% of radius to ensure it's inside
                
                // Convert distance to degrees (approximate)
                var latOffset = distance / 111.0; // 1 degree ≈ 111 km
                var lngOffset = distance / (111.0 * Math.Cos(searchLat * Math.PI / 180));
                
                businessLat = searchLat + latOffset * Math.Cos(angle);
                businessLng = searchLng + lngOffset * Math.Sin(angle);
            }
            else
            {
                // Place business outside radius
                var angle = random.NextDouble() * 2 * Math.PI;
                var distance = radiusKm * (1.2 + random.NextDouble()); // At least 20% beyond radius
                
                var latOffset = distance / 111.0;
                var lngOffset = distance / (111.0 * Math.Cos(searchLat * Math.PI / 180));
                
                businessLat = searchLat + latOffset * Math.Cos(angle);
                businessLng = searchLng + lngOffset * Math.Sin(angle);
            }

            // Ensure coordinates are valid
            businessLat = Math.Max(-89, Math.Min(89, businessLat));
            businessLng = Math.Max(-179, Math.Min(179, businessLng));

            var business = new Business
            {
                Id = Guid.NewGuid(),
                Name = $"Business {i + 1}",
                Email = new Email($"business{i + 1}@example.com"),
                Location = new Location((decimal)businessLat, (decimal)businessLng),
                IsActive = true
            };

            businesses.Add(business);
        }

        return (searchLat, searchLng, radiusKm, businesses);
    }

    private static Arbitrary<(double, double, double, double)> GenerateDistanceCalculationScenario()
    {
        return Arb.From(
            from lat1 in Gen.Choose(-89, 89).Select(x => (double)x)
            from lng1 in Gen.Choose(-179, 179).Select(x => (double)x)
            from lat2 in Gen.Choose(-89, 89).Select(x => (double)x)
            from lng2 in Gen.Choose(-179, 179).Select(x => (double)x)
            select (lat1, lng1, lat2, lng2));
    }

    private static Arbitrary<(double, double, double, List<(Business, List<Campaign>)>, bool)> GenerateBusinessFilteringScenario()
    {
        return Arb.From(
            from searchLat in Gen.Choose(-89, 89).Select(x => (double)x)
            from searchLng in Gen.Choose(-179, 179).Select(x => (double)x)
            from radiusKm in Gen.Choose(1, 20).Select(x => (double)x)
            from businessCount in Gen.Choose(1, 5)
            from activeOnly in Arb.Generate<bool>()
            select CreateBusinessFilteringScenario(searchLat, searchLng, radiusKm, businessCount, activeOnly));
    }

    private static (double, double, double, List<(Business, List<Campaign>)>, bool) CreateBusinessFilteringScenario(
        double searchLat, double searchLng, double radiusKm, int businessCount, bool activeOnly)
    {
        var businesses = new List<(Business, List<Campaign>)>();
        var random = new System.Random();

        for (int i = 0; i < businessCount; i++)
        {
            // Create business within radius
            var angle = random.NextDouble() * 2 * Math.PI;
            var distance = random.NextDouble() * radiusKm * 0.8; // Within radius
            
            var latOffset = distance / 111.0;
            var lngOffset = distance / (111.0 * Math.Cos(searchLat * Math.PI / 180));
            
            var businessLat = Math.Max(-89, Math.Min(89, searchLat + latOffset * Math.Cos(angle)));
            var businessLng = Math.Max(-179, Math.Min(179, searchLng + lngOffset * Math.Sin(angle)));

            var business = new Business
            {
                Id = Guid.NewGuid(),
                Name = $"Business {i + 1}",
                Email = new Email($"business{i + 1}@example.com"),
                Location = new Location((decimal)businessLat, (decimal)businessLng),
                IsActive = random.NextDouble() > 0.2 // 80% chance of being active
            };

            // Create campaigns for the business
            var campaigns = new List<Campaign>();
            var campaignCount = random.Next(0, 4); // 0-3 campaigns

            for (int j = 0; j < campaignCount; j++)
            {
                var isActive = random.NextDouble() > 0.3; // 70% chance of being active
                var startDate = DateTime.UtcNow.AddDays(random.Next(-30, -1));
                var endDate = random.NextDouble() > 0.5 
                    ? DateTime.UtcNow.AddDays(random.Next(1, 30))
                    : (DateTime?)null;

                var campaign = new Campaign
                {
                    Id = Guid.NewGuid(),
                    BusinessId = business.Id,
                    Name = $"Campaign {j + 1}",
                    TokenCostPerSpin = 5,
                    MaxSpinsPerDay = 10,
                    IsActive = isActive,
                    StartDate = startDate,
                    EndDate = endDate
                };

                campaigns.Add(campaign);
            }

            businesses.Add((business, campaigns));
        }

        return (searchLat, searchLng, radiusKm, businesses, activeOnly);
    }
}