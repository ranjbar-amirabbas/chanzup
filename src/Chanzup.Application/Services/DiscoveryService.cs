using Microsoft.EntityFrameworkCore;
using Chanzup.Application.DTOs;
using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;
using Chanzup.Domain.ValueObjects;

namespace Chanzup.Application.Services;

public class DiscoveryService : IDiscoveryService
{
    private readonly IApplicationDbContext _context;

    public DiscoveryService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NearbyBusinessesResponse> GetNearbyBusinessesAsync(
        double latitude, 
        double longitude, 
        double radiusKm = 5.0,
        string? category = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentException("Latitude must be between -90 and 90 degrees", nameof(latitude));
        
        if (longitude < -180 || longitude > 180)
            throw new ArgumentException("Longitude must be between -180 and 180 degrees", nameof(longitude));
        
        if (radiusKm <= 0 || radiusKm > 100)
            throw new ArgumentException("Radius must be between 0 and 100 kilometers", nameof(radiusKm));

        var searchLocation = new Location((decimal)latitude, (decimal)longitude);
        var radiusMeters = radiusKm * 1000;
        var now = DateTime.UtcNow;

        // Get businesses with active campaigns
        var query = _context.Businesses
            .Include(b => b.Campaigns)
                .ThenInclude(c => c.Prizes)
            .Where(b => b.IsActive && b.Location != null);

        if (activeOnly)
        {
            query = query.Where(b => b.Campaigns.Any(c => 
                c.IsActive && 
                c.StartDate <= now && 
                (c.EndDate == null || c.EndDate >= now)));
        }

        var businesses = await query.ToListAsync(cancellationToken);

        // Filter by distance and map to response
        var nearbyBusinesses = businesses
            .Where(b => b.Location!.IsWithinRadius(searchLocation, radiusMeters))
            .Select(b => MapToBusinessDiscoveryResponse(b, searchLocation, now, activeOnly))
            .OrderBy(b => b.DistanceKm)
            .ToList();

        // Apply category filter if specified
        if (!string.IsNullOrEmpty(category))
        {
            nearbyBusinesses = nearbyBusinesses
                .Where(b => string.Equals(b.Category, category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return new NearbyBusinessesResponse
        {
            SearchLocation = new LocationInfo 
            { 
                Latitude = (decimal)latitude, 
                Longitude = (decimal)longitude 
            },
            RadiusKm = radiusKm,
            Businesses = nearbyBusinesses,
            TotalBusinesses = nearbyBusinesses.Count,
            TotalCampaigns = nearbyBusinesses.Sum(b => b.ActiveCampaigns),
            Category = category,
            ActiveOnly = activeOnly
        };
    }

    public async Task<BusinessDiscoveryResponse?> GetBusinessDetailsAsync(
        Guid businessId,
        double? searchLatitude = null,
        double? searchLongitude = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var business = await _context.Businesses
            .Include(b => b.Campaigns)
                .ThenInclude(c => c.Prizes)
            .FirstOrDefaultAsync(b => b.Id == businessId && b.IsActive, cancellationToken);

        if (business == null)
            return null;

        Location? searchLocation = null;
        if (searchLatitude.HasValue && searchLongitude.HasValue)
        {
            searchLocation = new Location((decimal)searchLatitude.Value, (decimal)searchLongitude.Value);
        }

        return MapToBusinessDiscoveryResponse(business, searchLocation, now, true);
    }

    public async Task<List<CampaignResponse>> GetActiveCampaignsNearLocationAsync(
        double latitude,
        double longitude,
        double radiusKm = 10.0,
        CancellationToken cancellationToken = default)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentException("Latitude must be between -90 and 90 degrees", nameof(latitude));
        
        if (longitude < -180 || longitude > 180)
            throw new ArgumentException("Longitude must be between -180 and 180 degrees", nameof(longitude));

        var searchLocation = new Location((decimal)latitude, (decimal)longitude);
        var radiusMeters = radiusKm * 1000;
        var now = DateTime.UtcNow;

        var campaigns = await _context.Campaigns
            .Include(c => c.Prizes)
            .Include(c => c.Business)
            .Where(c => c.IsActive && 
                       c.StartDate <= now && 
                       (c.EndDate == null || c.EndDate >= now) &&
                       c.Business.IsActive &&
                       c.Business.Location != null)
            .ToListAsync(cancellationToken);

        // Filter by distance
        var nearbyCampaigns = campaigns
            .Where(c => c.Business.Location!.IsWithinRadius(searchLocation, radiusMeters))
            .Select(c => MapToCampaignResponse(c, searchLocation))
            .OrderBy(c => c.Business?.DistanceKm ?? double.MaxValue)
            .ToList();

        return nearbyCampaigns;
    }

    public async Task<DetailedCampaignResponse?> GetDetailedCampaignAsync(
        Guid campaignId,
        double? searchLatitude = null,
        double? searchLongitude = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var campaign = await _context.Campaigns
            .Include(c => c.Prizes)
            .Include(c => c.Business)
            .Include(c => c.WheelSpins)
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.IsActive, cancellationToken);

        if (campaign == null)
            return null;

        Location? searchLocation = null;
        if (searchLatitude.HasValue && searchLongitude.HasValue)
        {
            searchLocation = new Location((decimal)searchLatitude.Value, (decimal)searchLongitude.Value);
        }

        return MapToDetailedCampaignResponse(campaign, searchLocation, now);
    }

    public async Task<List<DetailedPrizeInfo>> GetCampaignPrizesAsync(
        Guid campaignId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Prizes
            .Where(p => p.CampaignId == campaignId);

        if (activeOnly)
        {
            query = query.Where(p => p.IsActive && p.RemainingQuantity > 0);
        }

        var prizes = await query.ToListAsync(cancellationToken);

        return prizes.Select(MapToDetailedPrizeInfo).ToList();
    }

    private static BusinessDiscoveryResponse MapToBusinessDiscoveryResponse(
        Business business, 
        Location? searchLocation, 
        DateTime now,
        bool activeOnly)
    {
        var activeCampaigns = business.Campaigns
            .Where(c => c.IsActive && 
                       c.StartDate <= now && 
                       (c.EndDate == null || c.EndDate >= now))
            .ToList();

        var campaignsToInclude = activeOnly ? activeCampaigns : business.Campaigns.ToList();

        var response = new BusinessDiscoveryResponse
        {
            BusinessId = business.Id,
            Name = business.Name,
            Address = business.Address,
            Location = new LocationInfo
            {
                Latitude = business.Location?.Latitude ?? 0,
                Longitude = business.Location?.Longitude ?? 0
            },
            ActiveCampaigns = activeCampaigns.Count,
            Campaigns = campaignsToInclude.Select(c => MapToCampaignSummary(c)).ToList(),
            Category = DetermineBusinessCategory(business.Name), // Simple categorization
            IsOpen = IsBusinessOpen(now), // Simplified - always open for now
            Hours = GetDefaultBusinessHours()
        };

        // Calculate distance if search location is provided
        if (searchLocation != null && business.Location != null)
        {
            var distanceMeters = business.Location.DistanceTo(searchLocation);
            response.DistanceKm = Math.Round(distanceMeters / 1000.0, 2);
        }

        return response;
    }

    private static CampaignSummary MapToCampaignSummary(Campaign campaign)
    {
        var activePrizes = campaign.Prizes
            .Where(p => p.IsActive && p.RemainingQuantity > 0)
            .ToList();

        var topPrizes = activePrizes
            .OrderByDescending(p => p.Value?.Amount ?? 0)
            .Take(3)
            .Select(p => new PrizeSummary
            {
                Name = p.Name,
                Value = p.Value?.Amount,
                RemainingQuantity = p.RemainingQuantity,
                IsSpecialPromotion = IsSpecialPromotion(p)
            })
            .ToList();

        return new CampaignSummary
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            GameType = campaign.GameType.ToString(),
            TokenCostPerSpin = campaign.TokenCostPerSpin,
            MaxSpinsPerDay = campaign.MaxSpinsPerDay,
            TopPrizes = topPrizes,
            AvailablePrizes = activePrizes.Count,
            TotalPrizeValue = activePrizes.Sum(p => (p.Value?.Amount ?? 0) * p.RemainingQuantity),
            HasSpecialPromotion = activePrizes.Any(IsSpecialPromotion)
        };
    }

    private static CampaignResponse MapToCampaignResponse(Campaign campaign, Location searchLocation)
    {
        var response = new CampaignResponse
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            GameType = campaign.GameType,
            TokenCostPerSpin = campaign.TokenCostPerSpin,
            MaxSpinsPerDay = campaign.MaxSpinsPerDay,
            IsActive = campaign.IsActive,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            CreatedAt = campaign.CreatedAt,
            UpdatedAt = campaign.UpdatedAt,
            Prizes = campaign.Prizes.Select(p => new PrizeResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Value = p.Value?.Amount,
                TotalQuantity = p.TotalQuantity,
                RemainingQuantity = p.RemainingQuantity,
                WinProbability = p.WinProbability,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList(),
            QRCodeUrl = $"/api/qr/{campaign.Id}"
        };

        // Include business information
        if (campaign.Business != null)
        {
            response.Business = new BusinessInfo
            {
                Id = campaign.Business.Id,
                Name = campaign.Business.Name,
                Address = campaign.Business.Address,
                Latitude = campaign.Business.Location?.Latitude,
                Longitude = campaign.Business.Location?.Longitude
            };

            // Calculate distance
            if (campaign.Business.Location != null)
            {
                var distanceMeters = campaign.Business.Location.DistanceTo(searchLocation);
                response.Business.DistanceKm = Math.Round(distanceMeters / 1000.0, 2);
            }
        }

        return response;
    }

    private static string DetermineBusinessCategory(string businessName)
    {
        // Simple categorization based on business name keywords
        var name = businessName.ToLowerInvariant();
        
        if (name.Contains("coffee") || name.Contains("cafe") || name.Contains("espresso"))
            return "Coffee & Tea";
        if (name.Contains("restaurant") || name.Contains("dining") || name.Contains("bistro"))
            return "Restaurant";
        if (name.Contains("shop") || name.Contains("store") || name.Contains("retail"))
            return "Retail";
        if (name.Contains("bar") || name.Contains("pub") || name.Contains("brewery"))
            return "Bar & Nightlife";
        if (name.Contains("gym") || name.Contains("fitness") || name.Contains("yoga"))
            return "Fitness & Recreation";
        
        return "Other";
    }

    private static bool IsBusinessOpen(DateTime now)
    {
        // Simplified logic - assume businesses are open during business hours
        var hour = now.Hour;
        var dayOfWeek = now.DayOfWeek;
        
        // Closed on Sundays
        if (dayOfWeek == DayOfWeek.Sunday)
            return false;
        
        // Open 9 AM to 9 PM on weekdays, 10 AM to 6 PM on Saturday
        if (dayOfWeek == DayOfWeek.Saturday)
            return hour >= 10 && hour < 18;
        
        return hour >= 9 && hour < 21;
    }

    private static BusinessHours GetDefaultBusinessHours()
    {
        return new BusinessHours
        {
            Monday = "9:00 AM - 9:00 PM",
            Tuesday = "9:00 AM - 9:00 PM",
            Wednesday = "9:00 AM - 9:00 PM",
            Thursday = "9:00 AM - 9:00 PM",
            Friday = "9:00 AM - 9:00 PM",
            Saturday = "10:00 AM - 6:00 PM",
            Sunday = "Closed"
        };
    }

    private static bool IsSpecialPromotion(Prize prize)
    {
        // Consider high-value prizes or limited quantity as special promotions
        return (prize.Value?.Amount ?? 0) > 20 || prize.RemainingQuantity <= 5;
    }

    private static DetailedCampaignResponse MapToDetailedCampaignResponse(
        Campaign campaign, 
        Location? searchLocation, 
        DateTime now)
    {
        var activePrizes = campaign.Prizes
            .Where(p => p.IsActive && p.RemainingQuantity > 0)
            .ToList();

        var response = new DetailedCampaignResponse
        {
            Id = campaign.Id,
            Name = campaign.Name,
            Description = campaign.Description,
            GameType = campaign.GameType.ToString(),
            TokenCostPerSpin = campaign.TokenCostPerSpin,
            MaxSpinsPerDay = campaign.MaxSpinsPerDay,
            IsActive = campaign.IsActive,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            CreatedAt = campaign.CreatedAt,
            UpdatedAt = campaign.UpdatedAt,
            QRCodeUrl = $"/api/qr/{campaign.Id}",
            
            // Detailed prize information
            Prizes = campaign.Prizes.Select(MapToDetailedPrizeInfo).ToList(),
            PrizeStats = CalculatePrizeStats(campaign.Prizes.ToList()),
            
            // Special promotions
            SpecialPromotions = GenerateSpecialPromotions(campaign, now),
            
            // Campaign rules
            Rules = new CampaignRules
            {
                TokenCostPerSpin = campaign.TokenCostPerSpin,
                MaxSpinsPerDay = campaign.MaxSpinsPerDay,
                MaxSpinsPerWeek = campaign.MaxSpinsPerDay * 7, // Simple calculation
                CooldownMinutes = 30, // Default cooldown
                RequiresLocationVerification = true,
                LocationRadiusMeters = 100, // 100m radius
                EligiblePlayerTypes = new List<string> { "all" },
                RestrictedRegions = new List<string>(),
                AgeRestriction = "18+"
            },
            
            // Player statistics (simplified)
            PlayerStats = CalculatePlayerStats(campaign)
        };

        // Include business information
        if (campaign.Business != null)
        {
            response.Business = new BusinessInfo
            {
                Id = campaign.Business.Id,
                Name = campaign.Business.Name,
                Address = campaign.Business.Address,
                Latitude = campaign.Business.Location?.Latitude,
                Longitude = campaign.Business.Location?.Longitude
            };

            // Calculate distance if search location is provided
            if (searchLocation != null && campaign.Business.Location != null)
            {
                var distanceMeters = campaign.Business.Location.DistanceTo(searchLocation);
                response.Business.DistanceKm = Math.Round(distanceMeters / 1000.0, 2);
            }
        }

        return response;
    }

    private static DetailedPrizeInfo MapToDetailedPrizeInfo(Prize prize)
    {
        return new DetailedPrizeInfo
        {
            Id = prize.Id,
            Name = prize.Name,
            Description = prize.Description,
            Value = prize.Value?.Amount,
            Currency = prize.Value?.Currency ?? "CAD",
            TotalQuantity = prize.TotalQuantity,
            RemainingQuantity = prize.RemainingQuantity,
            WinProbability = prize.WinProbability,
            IsActive = prize.IsActive,
            CreatedAt = prize.CreatedAt,
            UpdatedAt = prize.UpdatedAt,
            
            // Additional details
            ImageUrl = null, // Could be enhanced to include actual image URLs
            Terms = GeneratePrizeTerms(prize),
            ExpirationDate = prize.CreatedAt.AddDays(30), // Default 30-day expiration
            IsLimitedTime = prize.RemainingQuantity <= 10,
            IsSpecialPromotion = IsSpecialPromotion(prize),
            Category = DeterminePrizeCategory(prize.Name),
            PopularityScore = CalculatePopularityScore(prize)
        };
    }

    private static PrizeSummaryStats CalculatePrizeStats(List<Prize> prizes)
    {
        var activePrizes = prizes.Where(p => p.IsActive && p.RemainingQuantity > 0).ToList();
        var values = activePrizes.Where(p => p.Value?.Amount > 0).Select(p => p.Value!.Amount).ToList();

        return new PrizeSummaryStats
        {
            TotalPrizes = prizes.Count,
            AvailablePrizes = activePrizes.Count,
            TotalValue = activePrizes.Sum(p => (p.Value?.Amount ?? 0) * p.RemainingQuantity),
            AverageValue = values.Any() ? values.Average() : 0,
            HighestValue = values.Any() ? values.Max() : 0,
            LowestValue = values.Any() ? values.Min() : 0,
            MostPopularPrize = activePrizes.OrderByDescending(p => p.TotalQuantity - p.RemainingQuantity).FirstOrDefault()?.Name,
            TotalWinProbability = activePrizes.Sum(p => p.WinProbability)
        };
    }

    private static List<SpecialPromotion> GenerateSpecialPromotions(Campaign campaign, DateTime now)
    {
        var promotions = new List<SpecialPromotion>();

        // Check for limited-time campaign
        if (campaign.EndDate.HasValue && campaign.EndDate.Value.Subtract(now).TotalDays <= 7)
        {
            promotions.Add(new SpecialPromotion
            {
                Title = "Limited Time Campaign",
                Description = $"This campaign ends on {campaign.EndDate.Value:MMM dd, yyyy}!",
                Type = "limited_time",
                StartDate = campaign.StartDate,
                EndDate = campaign.EndDate,
                IsActive = true,
                BadgeText = "LIMITED TIME",
                BadgeColor = "#FF6B6B"
            });
        }

        // Check for high-value prizes
        var highValuePrizes = campaign.Prizes.Where(p => p.IsActive && (p.Value?.Amount ?? 0) > 50).ToList();
        if (highValuePrizes.Any())
        {
            promotions.Add(new SpecialPromotion
            {
                Title = "High Value Prizes Available",
                Description = $"Win prizes worth up to ${highValuePrizes.Max(p => p.Value?.Amount ?? 0):F2}!",
                Type = "high_value",
                IsActive = true,
                BadgeText = "HIGH VALUE",
                BadgeColor = "#4ECDC4"
            });
        }

        // Check for low inventory (creates urgency)
        var lowInventoryPrizes = campaign.Prizes.Where(p => p.IsActive && p.RemainingQuantity <= 5 && p.RemainingQuantity > 0).ToList();
        if (lowInventoryPrizes.Any())
        {
            promotions.Add(new SpecialPromotion
            {
                Title = "Limited Quantities Remaining",
                Description = "Some prizes are running low - spin now before they're gone!",
                Type = "limited_quantity",
                IsActive = true,
                BadgeText = "ALMOST GONE",
                BadgeColor = "#FFE66D"
            });
        }

        return promotions;
    }

    private static CampaignPlayerStats CalculatePlayerStats(Campaign campaign)
    {
        // Simplified stats calculation - in a real implementation, this would query actual data
        var totalSpins = campaign.WheelSpins?.Count ?? 0;
        var uniquePlayers = campaign.WheelSpins?.Select(ws => ws.PlayerId).Distinct().Count() ?? 0;
        var prizesWon = campaign.WheelSpins?.Count(ws => ws.PrizeId != null) ?? 0;

        return new CampaignPlayerStats
        {
            TotalPlayers = uniquePlayers,
            ActivePlayersToday = Math.Max(1, uniquePlayers / 7), // Rough estimate
            TotalSpins = totalSpins,
            SpinsToday = Math.Max(0, totalSpins / 30), // Rough daily average
            AverageSpinsPerPlayer = uniquePlayers > 0 ? (decimal)totalSpins / uniquePlayers : 0,
            TotalPrizesWon = prizesWon,
            PrizesWonToday = Math.Max(0, prizesWon / 30), // Rough daily average
            RedemptionRate = totalSpins > 0 ? (decimal)prizesWon / totalSpins : 0
        };
    }

    private static string GeneratePrizeTerms(Prize prize)
    {
        var terms = new List<string>
        {
            "Prize must be redeemed within 30 days of winning",
            "Valid at participating location only",
            "Cannot be combined with other offers",
            "No cash value"
        };

        if (prize.Value?.Amount > 0)
        {
            terms.Add($"Prize value: ${prize.Value.Amount:F2} {prize.Value.Currency}");
        }

        return string.Join(". ", terms) + ".";
    }

    private static string DeterminePrizeCategory(string prizeName)
    {
        var name = prizeName.ToLowerInvariant();
        
        if (name.Contains("coffee") || name.Contains("drink") || name.Contains("beverage"))
            return "Food & Beverage";
        if (name.Contains("discount") || name.Contains("%") || name.Contains("off"))
            return "Discount";
        if (name.Contains("free") || name.Contains("complimentary"))
            return "Free Item";
        if (name.Contains("gift") || name.Contains("card"))
            return "Gift Card";
        if (name.Contains("merchandise") || name.Contains("shirt") || name.Contains("mug"))
            return "Merchandise";
        
        return "Other";
    }

    private static int CalculatePopularityScore(Prize prize)
    {
        // Simple popularity calculation based on how much has been claimed
        var claimedPercentage = prize.TotalQuantity > 0 
            ? (double)(prize.TotalQuantity - prize.RemainingQuantity) / prize.TotalQuantity 
            : 0;
        
        return (int)(claimedPercentage * 100);
    }
}