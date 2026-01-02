namespace Chanzup.Application.DTOs;

public class MultiLocationBusinessMetrics
{
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalLocations { get; set; }
    public int ActiveLocations { get; set; }
    
    // Consolidated metrics across all locations
    public BusinessMetrics ConsolidatedMetrics { get; set; } = new();
    
    // Individual location metrics
    public List<LocationMetrics> LocationMetrics { get; set; } = new();
    
    // Performance comparison
    public LocationMetrics? TopPerformingLocation { get; set; }
    public LocationMetrics? LowestPerformingLocation { get; set; }
    public LocationPerformanceComparison LocationPerformanceComparison { get; set; } = new();
}

public class LocationMetrics
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int QRScans { get; set; }
    public int Spins { get; set; }
    public int UniquePlayers { get; set; }
    public int PrizesAwarded { get; set; }
    public int PrizesRedeemed { get; set; }
    public decimal RedemptionRate { get; set; }
    public int TokenRevenue { get; set; }
    public double AverageSpinsPerPlayer { get; set; }
}

public class CampaignLocationPerformance
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public Guid CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public int QRScans { get; set; }
    public int Spins { get; set; }
    public int UniquePlayers { get; set; }
    public int PrizesAwarded { get; set; }
    public int PrizesRedeemed { get; set; }
    public decimal RedemptionRate { get; set; }
    public int TokenRevenue { get; set; }
    public double AverageSpinsPerPlayer { get; set; }
    public decimal ConversionRate { get; set; } // QR scans to spins
}

public class LocationPerformanceComparison
{
    public double AverageQRScansPerLocation { get; set; }
    public double AverageSpinsPerLocation { get; set; }
    public double AverageRevenuePerLocation { get; set; }
    public decimal AverageRedemptionRate { get; set; }
    public LocationPerformanceVariance PerformanceVariance { get; set; } = new();
    public List<LocationRanking> LocationRankings { get; set; } = new();
}

public class LocationPerformanceVariance
{
    public double QRScansVariance { get; set; }
    public double SpinsVariance { get; set; }
    public double RevenueVariance { get; set; }
    public double RedemptionRateVariance { get; set; }
}

public class LocationRanking
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int Rank { get; set; }
    public double Score { get; set; } // Composite performance score
}

public class BusinessMetrics
{
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public int TotalQRScans { get; set; }
    public int TotalSpins { get; set; }
    public int UniquePlayers { get; set; }
    public int TotalPrizesAwarded { get; set; }
    public int TotalPrizesRedeemed { get; set; }
    public decimal OverallRedemptionRate { get; set; }
    public int TotalTokenRevenue { get; set; }
    public int ActiveCampaigns { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

public class CampaignMetrics
{
    public Guid CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public int TotalSpins { get; set; }
    public int UniquePlayers { get; set; }
    public int PrizesAwarded { get; set; }
    public int PrizesRedeemed { get; set; }
    public decimal RedemptionRate { get; set; }
    public int TokenRevenue { get; set; }
    public double AverageSpinsPerPlayer { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

public class PlayerEngagementMetrics
{
    public Guid BusinessId { get; set; }
    public int TotalPlayers { get; set; }
    public int ActivePlayersToday { get; set; }
    public int ActivePlayersThisWeek { get; set; }
    public int ActivePlayersThisMonth { get; set; }
    public double AverageSessionsPerPlayer { get; set; }
    public double AverageSpinsPerPlayer { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

public class DailyMetrics
{
    public DateTime Date { get; set; }
    public int QRScans { get; set; }
    public int Spins { get; set; }
    public int UniquePlayers { get; set; }
    public int PrizesAwarded { get; set; }
    public int PrizesRedeemed { get; set; }
    public int TokensEarned { get; set; }
    public int TokensSpent { get; set; }
}