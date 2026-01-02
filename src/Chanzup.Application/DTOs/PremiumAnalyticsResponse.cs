namespace Chanzup.Application.DTOs;

/// <summary>
/// Premium analytics response with advanced metrics and insights
/// Available only for Premium and Enterprise subscription tiers
/// </summary>
public class PremiumAnalyticsResponse
{
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string SubscriptionTier { get; set; } = string.Empty;
    
    // Advanced Performance Metrics
    public AdvancedPerformanceMetrics Performance { get; set; } = new();
    
    // Predictive Analytics
    public PredictiveAnalytics Predictions { get; set; } = new();
    
    // Competitive Benchmarking
    public CompetitiveBenchmarks Benchmarks { get; set; } = new();
    
    // Advanced Player Insights
    public AdvancedPlayerInsights PlayerInsights { get; set; } = new();
    
    // Revenue Analytics
    public RevenueAnalytics Revenue { get; set; } = new();
    
    // Campaign Optimization Recommendations
    public List<OptimizationRecommendation> Recommendations { get; set; } = new();
}

public class AdvancedPerformanceMetrics
{
    // ROI and Financial Metrics
    public decimal ReturnOnInvestment { get; set; }
    public decimal CustomerAcquisitionCost { get; set; }
    public decimal CustomerLifetimeValue { get; set; }
    public decimal RevenuePerPlayer { get; set; }
    public decimal ProfitMargin { get; set; }
    
    // Advanced Engagement Metrics
    public double EngagementScore { get; set; } // Composite score 0-100
    public double StickinessIndex { get; set; } // DAU/MAU ratio
    public double ViralityCoefficient { get; set; } // Referral effectiveness
    public TimeSpan AverageTimeToFirstWin { get; set; }
    public TimeSpan AverageTimeToRedemption { get; set; }
    
    // Conversion Optimization
    public decimal OptimalSpinPrice { get; set; }
    public Dictionary<string, decimal> ConversionRatesBySegment { get; set; } = new();
    public Dictionary<int, decimal> ConversionRatesByHour { get; set; } = new();
    public Dictionary<string, decimal> ConversionRatesByDayOfWeek { get; set; } = new();
}

public class PredictiveAnalytics
{
    // Player Behavior Predictions
    public int PredictedNewPlayersNextMonth { get; set; }
    public decimal PredictedChurnRate { get; set; }
    public int PredictedActivePlayersNextWeek { get; set; }
    
    // Revenue Predictions
    public decimal PredictedRevenueNextMonth { get; set; }
    public decimal PredictedTokenSalesNextWeek { get; set; }
    
    // Campaign Performance Predictions
    public Dictionary<Guid, CampaignPrediction> CampaignPredictions { get; set; } = new();
    
    // Seasonal Trends
    public List<SeasonalTrend> SeasonalTrends { get; set; } = new();
    
    // Risk Indicators
    public List<RiskIndicator> RiskIndicators { get; set; } = new();
}

public class CampaignPrediction
{
    public Guid CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public int PredictedSpinsNextWeek { get; set; }
    public decimal PredictedRevenueNextWeek { get; set; }
    public decimal PredictedCompletionDate { get; set; }
    public string PerformanceOutlook { get; set; } = string.Empty; // "Excellent", "Good", "Poor"
}

public class SeasonalTrend
{
    public string Period { get; set; } = string.Empty; // "Holiday", "Summer", "Weekend"
    public decimal ImpactMultiplier { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime NextOccurrence { get; set; }
}

public class RiskIndicator
{
    public string Type { get; set; } = string.Empty; // "ChurnRisk", "RevenueDecline", "EngagementDrop"
    public string Severity { get; set; } = string.Empty; // "Low", "Medium", "High", "Critical"
    public string Description { get; set; } = string.Empty;
    public List<string> RecommendedActions { get; set; } = new();
    public decimal Probability { get; set; }
}

public class CompetitiveBenchmarks
{
    // Industry Benchmarks (anonymized)
    public decimal IndustryAverageRedemptionRate { get; set; }
    public double IndustryAverageEngagementScore { get; set; }
    public decimal IndustryAverageRevenuePerPlayer { get; set; }
    public TimeSpan IndustryAverageSessionDuration { get; set; }
    
    // Performance Comparison
    public string RedemptionRateRanking { get; set; } = string.Empty; // "Top 10%", "Above Average", etc.
    public string EngagementRanking { get; set; } = string.Empty;
    public string RevenueRanking { get; set; } = string.Empty;
    
    // Market Position
    public string MarketPosition { get; set; } = string.Empty; // "Leader", "Challenger", "Follower"
    public List<string> CompetitiveAdvantages { get; set; } = new();
    public List<string> ImprovementOpportunities { get; set; } = new();
}

public class AdvancedPlayerInsights
{
    // Player Lifetime Value Analysis
    public Dictionary<string, decimal> LifetimeValueBySegment { get; set; } = new();
    public Dictionary<string, int> PlayerCountBySegment { get; set; } = new();
    
    // Behavioral Cohort Analysis
    public List<CohortAnalysis> CohortAnalysis { get; set; } = new();
    
    // Player Journey Analytics
    public PlayerJourneyMetrics PlayerJourney { get; set; } = new();
    
    // Churn Analysis
    public ChurnAnalysis ChurnAnalysis { get; set; } = new();
    
    // Player Satisfaction Metrics
    public PlayerSatisfactionMetrics Satisfaction { get; set; } = new();
}

public class CohortAnalysis
{
    public DateTime CohortMonth { get; set; }
    public int InitialPlayerCount { get; set; }
    public Dictionary<int, decimal> RetentionByMonth { get; set; } = new(); // Month -> Retention Rate
    public Dictionary<int, decimal> RevenueByMonth { get; set; } = new(); // Month -> Revenue
}

public class PlayerJourneyMetrics
{
    public TimeSpan AverageTimeToFirstSpin { get; set; }
    public TimeSpan AverageTimeToFirstWin { get; set; }
    public TimeSpan AverageTimeToFirstRedemption { get; set; }
    public decimal FirstSpinConversionRate { get; set; }
    public decimal FirstWinConversionRate { get; set; }
    public Dictionary<string, int> DropoffPointsByStage { get; set; } = new();
}

public class ChurnAnalysis
{
    public decimal CurrentChurnRate { get; set; }
    public decimal PredictedChurnRate { get; set; }
    public List<ChurnRiskFactor> ChurnRiskFactors { get; set; } = new();
    public List<Guid> PlayersAtRisk { get; set; } = new();
    public Dictionary<string, decimal> ChurnRateBySegment { get; set; } = new();
}

public class ChurnRiskFactor
{
    public string Factor { get; set; } = string.Empty;
    public decimal Impact { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class PlayerSatisfactionMetrics
{
    public double OverallSatisfactionScore { get; set; } // 0-100
    public double GameplayExperienceScore { get; set; }
    public double PrizeQualityScore { get; set; }
    public double RedemptionExperienceScore { get; set; }
    public List<string> TopCompliments { get; set; } = new();
    public List<string> TopComplaints { get; set; } = new();
}

public class RevenueAnalytics
{
    // Revenue Breakdown
    public decimal TotalRevenue { get; set; }
    public decimal TokenSalesRevenue { get; set; }
    public decimal SubscriptionRevenue { get; set; }
    public decimal CommissionRevenue { get; set; }
    
    // Revenue Trends
    public List<RevenueByPeriod> DailyRevenue { get; set; } = new();
    public List<RevenueByPeriod> WeeklyRevenue { get; set; } = new();
    public List<RevenueByPeriod> MonthlyRevenue { get; set; } = new();
    
    // Revenue Optimization
    public decimal OptimalTokenPrice { get; set; }
    public Dictionary<string, decimal> RevenueByPlayerSegment { get; set; } = new();
    public Dictionary<string, decimal> RevenueByPrizeCategory { get; set; } = new();
    
    // Financial Health Indicators
    public decimal RevenueGrowthRate { get; set; }
    public decimal RevenueVolatility { get; set; }
    public decimal SeasonalityIndex { get; set; }
}

public class RevenueByPeriod
{
    public DateTime Period { get; set; }
    public decimal Revenue { get; set; }
    public decimal GrowthRate { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTransactionValue { get; set; }
}

public class OptimizationRecommendation
{
    public string Category { get; set; } = string.Empty; // "Pricing", "Campaigns", "Player Engagement", etc.
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty; // "High", "Medium", "Low"
    public decimal PotentialImpact { get; set; } // Expected improvement percentage
    public string ImpactType { get; set; } = string.Empty; // "Revenue", "Engagement", "Retention"
    public List<string> ActionItems { get; set; } = new();
    public int EstimatedImplementationDays { get; set; }
    public decimal ConfidenceScore { get; set; } // 0-1
}

/// <summary>
/// Advanced campaign comparison analytics for Premium+ subscribers
/// </summary>
public class AdvancedCampaignComparisonResponse
{
    public List<Guid> CampaignIds { get; set; } = new();
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    
    // Statistical Analysis
    public CampaignStatisticalAnalysis StatisticalAnalysis { get; set; } = new();
    
    // Performance Ranking
    public List<CampaignRanking> PerformanceRankings { get; set; } = new();
    
    // Cross-Campaign Insights
    public CrossCampaignInsights CrossCampaignInsights { get; set; } = new();
    
    // Optimization Opportunities
    public List<CampaignOptimizationOpportunity> OptimizationOpportunities { get; set; } = new();
}

public class CampaignStatisticalAnalysis
{
    public decimal AverageRedemptionRate { get; set; }
    public decimal StandardDeviationRedemptionRate { get; set; }
    public decimal AverageEngagementScore { get; set; }
    public decimal StandardDeviationEngagementScore { get; set; }
    public string BestPerformingMetric { get; set; } = string.Empty;
    public string WorstPerformingMetric { get; set; } = string.Empty;
    public decimal CorrelationSpinsToRevenue { get; set; }
    public decimal CorrelationPrizeValueToRedemption { get; set; }
}

public class CampaignRanking
{
    public Guid CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public int OverallRank { get; set; }
    public Dictionary<string, int> MetricRankings { get; set; } = new(); // Metric -> Rank
    public double OverallScore { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
}

public class CrossCampaignInsights
{
    public string MostSuccessfulCampaignType { get; set; } = string.Empty;
    public string OptimalCampaignDuration { get; set; } = string.Empty;
    public decimal OptimalPrizeValueRange { get; set; }
    public string BestPerformingTimeSlot { get; set; } = string.Empty;
    public List<string> SuccessPatterns { get; set; } = new();
    public List<string> FailurePatterns { get; set; } = new();
}

public class CampaignOptimizationOpportunity
{
    public Guid CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string OpportunityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PotentialImprovement { get; set; }
    public string RecommendedAction { get; set; } = string.Empty;
    public int Priority { get; set; } // 1-5
}