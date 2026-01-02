using Chanzup.Domain.Entities;

namespace Chanzup.Application.DTOs;

public class DetailedCampaignResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string GameType { get; set; } = string.Empty;
    public int TokenCostPerSpin { get; set; }
    public int MaxSpinsPerDay { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? QRCodeUrl { get; set; }
    
    // Business Information
    public BusinessInfo Business { get; set; } = new();
    
    // Detailed Prize Information
    public List<DetailedPrizeInfo> Prizes { get; set; } = new();
    public PrizeSummaryStats PrizeStats { get; set; } = new();
    
    // Special Promotions
    public List<SpecialPromotion> SpecialPromotions { get; set; } = new();
    
    // Campaign Rules and Restrictions
    public CampaignRules Rules { get; set; } = new();
    
    // Player Statistics (if available)
    public CampaignPlayerStats? PlayerStats { get; set; }
}

public class DetailedPrizeInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Value { get; set; }
    public string? Currency { get; set; } = "CAD";
    public int TotalQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public decimal WinProbability { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Additional Prize Details
    public string? ImageUrl { get; set; }
    public string? Terms { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public bool IsLimitedTime { get; set; }
    public bool IsSpecialPromotion { get; set; }
    public string? Category { get; set; }
    public int PopularityScore { get; set; } // Based on how often it's won/redeemed
}

public class PrizeSummaryStats
{
    public int TotalPrizes { get; set; }
    public int AvailablePrizes { get; set; }
    public decimal TotalValue { get; set; }
    public decimal AverageValue { get; set; }
    public decimal HighestValue { get; set; }
    public decimal LowestValue { get; set; }
    public string? MostPopularPrize { get; set; }
    public decimal TotalWinProbability { get; set; }
}

public class SpecialPromotion
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "limited_time", "high_value", "bonus_spins", etc.
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public string? BadgeText { get; set; } // "LIMITED TIME", "HIGH VALUE", etc.
    public string? BadgeColor { get; set; } // For UI styling
}

public class CampaignRules
{
    public int TokenCostPerSpin { get; set; }
    public int MaxSpinsPerDay { get; set; }
    public int MaxSpinsPerWeek { get; set; }
    public int CooldownMinutes { get; set; }
    public bool RequiresLocationVerification { get; set; }
    public double LocationRadiusMeters { get; set; }
    public List<string> EligiblePlayerTypes { get; set; } = new();
    public List<string> RestrictedRegions { get; set; } = new();
    public string? AgeRestriction { get; set; }
}

public class CampaignPlayerStats
{
    public int TotalPlayers { get; set; }
    public int ActivePlayersToday { get; set; }
    public int TotalSpins { get; set; }
    public int SpinsToday { get; set; }
    public decimal AverageSpinsPerPlayer { get; set; }
    public int TotalPrizesWon { get; set; }
    public int PrizesWonToday { get; set; }
    public decimal RedemptionRate { get; set; }
}