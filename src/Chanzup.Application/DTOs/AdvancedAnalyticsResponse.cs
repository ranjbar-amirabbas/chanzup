using Chanzup.Application.Interfaces;

namespace Chanzup.Application.DTOs;

public class DetailedCampaignPerformanceResponse
{
    public Guid CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    
    // Performance Metrics
    public CampaignPerformanceMetrics Performance { get; set; } = new();
    
    // Prize Analytics
    public List<PrizePerformanceMetrics> PrizePerformance { get; set; } = new();
    
    // Time-based Analytics
    public List<HourlyMetrics> HourlyBreakdown { get; set; } = new();
    public List<DailyMetrics> DailyBreakdown { get; set; } = new();
    
    // Player Segmentation
    public PlayerSegmentationMetrics PlayerSegmentation { get; set; } = new();
    
    // Conversion Funnel
    public ConversionFunnelMetrics ConversionFunnel { get; set; } = new();
}

public class CampaignPerformanceMetrics
{
    public int TotalSpins { get; set; }
    public int UniquePlayers { get; set; }
    public int NewPlayers { get; set; }
    public int ReturningPlayers { get; set; }
    public int PrizesAwarded { get; set; }
    public int PrizesRedeemed { get; set; }
    public decimal RedemptionRate { get; set; }
    public int TokenRevenue { get; set; }
    public double AverageSpinsPerPlayer { get; set; }
    public double AverageSpinsPerSession { get; set; }
    public decimal WinRate { get; set; }
    public decimal AveragePrizeValue { get; set; }
    public TimeSpan AverageSessionDuration { get; set; }
    public int TotalSessions { get; set; }
    public double SessionsPerPlayer { get; set; }
}

public class PrizePerformanceMetrics
{
    public Guid PrizeId { get; set; }
    public string PrizeName { get; set; } = string.Empty;
    public decimal PrizeValue { get; set; }
    public int TotalQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public int TimesWon { get; set; }
    public int TimesRedeemed { get; set; }
    public decimal WinRate { get; set; }
    public decimal RedemptionRate { get; set; }
    public decimal ConfiguredProbability { get; set; }
    public decimal ActualWinProbability { get; set; }
    public decimal PopularityScore { get; set; }
}

public class HourlyMetrics
{
    public int Hour { get; set; }
    public int Spins { get; set; }
    public int UniquePlayers { get; set; }
    public int PrizesAwarded { get; set; }
    public int TokensSpent { get; set; }
}

public class PlayerSegmentationMetrics
{
    public int LowEngagementPlayers { get; set; } // 1-2 spins
    public int MediumEngagementPlayers { get; set; } // 3-10 spins
    public int HighEngagementPlayers { get; set; } // 11+ spins
    public double AverageSpinsLowEngagement { get; set; }
    public double AverageSpinsMediumEngagement { get; set; }
    public double AverageSpinsHighEngagement { get; set; }
    public decimal RedemptionRateLowEngagement { get; set; }
    public decimal RedemptionRateMediumEngagement { get; set; }
    public decimal RedemptionRateHighEngagement { get; set; }
}

public class ConversionFunnelMetrics
{
    public int QRScans { get; set; }
    public int PlayersWhoSpun { get; set; }
    public int PlayersWhoWonPrizes { get; set; }
    public int PlayersWhoRedeemed { get; set; }
    public decimal ScanToSpinConversion { get; set; }
    public decimal SpinToWinConversion { get; set; }
    public decimal WinToRedemptionConversion { get; set; }
    public decimal OverallConversion { get; set; }
}

public class ComprehensivePlayerBehaviorResponse
{
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    
    // Player Demographics
    public PlayerDemographicsMetrics Demographics { get; set; } = new();
    
    // Engagement Patterns
    public PlayerEngagementPatterns EngagementPatterns { get; set; } = new();
    
    // Behavioral Insights
    public PlayerBehavioralInsights BehavioralInsights { get; set; } = new();
    
    // Retention Analysis
    public PlayerRetentionMetrics Retention { get; set; } = new();
    
    // Geographic Analysis
    public List<GeographicMetrics> GeographicBreakdown { get; set; } = new();
    
    // Top Players
    public List<TopPlayerMetrics> TopPlayers { get; set; } = new();
}

public class PlayerDemographicsMetrics
{
    public int TotalPlayers { get; set; }
    public int NewPlayersThisPeriod { get; set; }
    public int ActivePlayersToday { get; set; }
    public int ActivePlayersThisWeek { get; set; }
    public int ActivePlayersThisMonth { get; set; }
    public double AveragePlayerAge { get; set; }
    public Dictionary<string, int> PlayersByRegistrationSource { get; set; } = new();
}

public class PlayerEngagementPatterns
{
    public Dictionary<int, int> SpinsByHour { get; set; } = new(); // Hour -> Count
    public Dictionary<string, int> SpinsByDayOfWeek { get; set; } = new(); // DayName -> Count
    public double AverageSessionDuration { get; set; }
    public double AverageSpinsPerSession { get; set; }
    public double AverageTimeBetweenSessions { get; set; }
    public int PeakHour { get; set; }
    public string PeakDay { get; set; } = string.Empty;
}

public class PlayerBehavioralInsights
{
    public decimal AverageTokenBalance { get; set; }
    public int PlayersWithZeroBalance { get; set; }
    public int PlayersWithHighBalance { get; set; } // >50 tokens
    public double AverageSpinsBeforeFirstWin { get; set; }
    public double AverageTimeBetweenWinAndRedemption { get; set; }
    public decimal ChurnRate { get; set; } // Players inactive for 30+ days
    public List<string> MostPopularPrizes { get; set; } = new();
    public List<string> LeastPopularPrizes { get; set; } = new();
}

public class PlayerRetentionMetrics
{
    public decimal Day1Retention { get; set; }
    public decimal Day7Retention { get; set; }
    public decimal Day30Retention { get; set; }
    public decimal MonthlyChurnRate { get; set; }
    public double AveragePlayerLifetime { get; set; } // Days
    public int ReturningPlayersThisMonth { get; set; }
    public int LapsedPlayersReactivated { get; set; }
}

public class GeographicMetrics
{
    public string Region { get; set; } = string.Empty;
    public int PlayerCount { get; set; }
    public int TotalSpins { get; set; }
    public decimal AverageSpinsPerPlayer { get; set; }
    public decimal RedemptionRate { get; set; }
}

public class TopPlayerMetrics
{
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty; // Anonymized
    public int TotalSpins { get; set; }
    public int PrizesWon { get; set; }
    public int PrizesRedeemed { get; set; }
    public int TokensSpent { get; set; }
    public DateTime LastActivity { get; set; }
    public int DaysActive { get; set; }
}

public class ExportableReportResponse
{
    public string ReportId { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty; // CSV, PDF, Excel
    public DateTime GeneratedAt { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public ReportMetadata Metadata { get; set; } = new();
}

public class ReportMetadata
{
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalRecords { get; set; }
    public List<string> IncludedMetrics { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class ReportGenerationRequest
{
    public string ReportType { get; set; } = string.Empty; // "campaign", "business", "player-behavior"
    public string Format { get; set; } = "CSV"; // CSV, PDF, Excel
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<string> IncludeMetrics { get; set; } = new();
    public Dictionary<string, object> Filters { get; set; } = new();
    public bool IncludeCharts { get; set; } = false;
    public bool IncludeRawData { get; set; } = true;
}