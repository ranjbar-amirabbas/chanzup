using Chanzup.Domain.Entities;
using Chanzup.Application.DTOs;

namespace Chanzup.Application.Interfaces;

public interface IAnalyticsService
{
    // Event tracking methods
    Task TrackQRScanAsync(Guid businessId, Guid playerId, Guid campaignId, int tokensEarned, CancellationToken cancellationToken = default);
    Task TrackWheelSpinAsync(Guid businessId, Guid playerId, Guid campaignId, int tokensSpent, bool wonPrize, Guid? prizeId = null, CancellationToken cancellationToken = default);
    Task TrackPrizeRedemptionAsync(Guid businessId, Guid playerId, Guid prizeId, CancellationToken cancellationToken = default);
    Task TrackPlayerRegistrationAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task TrackCampaignCreatedAsync(Guid businessId, Guid campaignId, CancellationToken cancellationToken = default);
    Task TrackCustomEventAsync(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default);

    // Basic metrics calculation methods
    Task<CampaignMetrics> GetCampaignMetricsAsync(Guid campaignId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<BusinessMetrics> GetBusinessMetricsAsync(Guid businessId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<PlayerEngagementMetrics> GetPlayerEngagementMetricsAsync(Guid businessId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<List<DailyMetrics>> GetDailyMetricsAsync(Guid businessId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    // Advanced analytics methods
    Task<DetailedCampaignPerformanceResponse> GetDetailedCampaignPerformanceAsync(Guid campaignId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<ComprehensivePlayerBehaviorResponse> GetComprehensivePlayerBehaviorAsync(Guid businessId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<ExportableReportResponse> GenerateExportableReportAsync(Guid businessId, ReportGenerationRequest request, CancellationToken cancellationToken = default);
    Task<List<DetailedCampaignPerformanceResponse>> GetAllCampaignPerformancesAsync(Guid businessId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

    // Premium analytics methods (Premium+ subscription required)
    Task<PremiumAnalyticsResponse> GetPremiumAnalyticsAsync(Guid businessId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<AdvancedCampaignComparisonResponse> GetAdvancedCampaignComparisonAsync(Guid businessId, List<Guid> campaignIds, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<List<OptimizationRecommendation>> GetOptimizationRecommendationsAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task<PredictiveAnalytics> GetPredictiveAnalyticsAsync(Guid businessId, CancellationToken cancellationToken = default);

    // Multi-location analytics methods
    Task<MultiLocationBusinessMetrics> GetMultiLocationBusinessMetricsAsync(Guid businessId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<List<CampaignLocationPerformance>> GetCampaignLocationPerformanceAsync(Guid campaignId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
}