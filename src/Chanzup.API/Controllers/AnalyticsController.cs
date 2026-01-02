using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Chanzup.Application.Interfaces;
using Chanzup.Application.DTOs;
using Chanzup.API.Authorization;
using Chanzup.Domain.Entities;

namespace Chanzup.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Get campaign performance metrics
    /// </summary>
    [HttpGet("campaigns/{campaignId}")]
    [RequirePermission("analytics:read")]
    public async Task<ActionResult<CampaignMetrics>> GetCampaignMetrics(
        Guid campaignId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _analyticsService.GetCampaignMetricsAsync(campaignId, startDate, endDate, cancellationToken);
            return Ok(metrics);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Get business performance metrics
    /// </summary>
    [HttpGet("businesses/{businessId}")]
    [RequirePermission("analytics:read")]
    public async Task<ActionResult<BusinessMetrics>> GetBusinessMetrics(
        Guid businessId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _analyticsService.GetBusinessMetricsAsync(businessId, startDate, endDate, cancellationToken);
            return Ok(metrics);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Get player engagement metrics for a business
    /// </summary>
    [HttpGet("businesses/{businessId}/engagement")]
    [RequirePermission("analytics:read")]
    public async Task<ActionResult<PlayerEngagementMetrics>> GetPlayerEngagementMetrics(
        Guid businessId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var metrics = await _analyticsService.GetPlayerEngagementMetricsAsync(businessId, startDate, endDate, cancellationToken);
        return Ok(metrics);
    }

    /// <summary>
    /// Get daily metrics breakdown for a business
    /// </summary>
    [HttpGet("businesses/{businessId}/daily")]
    [RequirePermission("analytics:read")]
    public async Task<ActionResult<List<DailyMetrics>>> GetDailyMetrics(
        Guid businessId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (startDate == default || endDate == default)
        {
            return BadRequest("Both startDate and endDate are required for daily metrics");
        }

        if (endDate < startDate)
        {
            return BadRequest("endDate must be greater than or equal to startDate");
        }

        var metrics = await _analyticsService.GetDailyMetricsAsync(businessId, startDate, endDate, cancellationToken);
        return Ok(metrics);
    }

    /// <summary>
    /// Get detailed campaign performance analytics with comprehensive metrics
    /// </summary>
    [HttpGet("campaigns/{campaignId}/detailed")]
    [RequirePermission("analytics:read")]
    public async Task<ActionResult<DetailedCampaignPerformanceResponse>> GetDetailedCampaignPerformance(
        Guid campaignId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var performance = await _analyticsService.GetDetailedCampaignPerformanceAsync(campaignId, startDate, endDate, cancellationToken);
            return Ok(performance);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Get comprehensive player behavior analytics for a business
    /// </summary>
    [HttpGet("businesses/{businessId}/player-behavior")]
    [RequirePermission("analytics:read")]
    public async Task<ActionResult<ComprehensivePlayerBehaviorResponse>> GetComprehensivePlayerBehavior(
        Guid businessId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var behavior = await _analyticsService.GetComprehensivePlayerBehaviorAsync(businessId, startDate, endDate, cancellationToken);
            return Ok(behavior);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Get detailed performance for all campaigns of a business
    /// </summary>
    [HttpGet("businesses/{businessId}/campaigns/detailed")]
    [RequirePermission("analytics:read")]
    public async Task<ActionResult<List<DetailedCampaignPerformanceResponse>>> GetAllCampaignPerformances(
        Guid businessId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var performances = await _analyticsService.GetAllCampaignPerformancesAsync(businessId, startDate, endDate, cancellationToken);
        return Ok(performances);
    }

    /// <summary>
    /// Generate an exportable report for business analytics
    /// </summary>
    [HttpPost("businesses/{businessId}/reports/generate")]
    [RequirePermission("analytics:export")]
    public async Task<ActionResult<ExportableReportResponse>> GenerateExportableReport(
        Guid businessId,
        [FromBody] ReportGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.StartDate == default || request.EndDate == default)
        {
            return BadRequest("Both StartDate and EndDate are required for report generation");
        }

        if (request.EndDate < request.StartDate)
        {
            return BadRequest("EndDate must be greater than or equal to StartDate");
        }

        if (string.IsNullOrEmpty(request.ReportType))
        {
            return BadRequest("ReportType is required");
        }

        var validReportTypes = new[] { "campaign", "business", "player-behavior" };
        if (!validReportTypes.Contains(request.ReportType.ToLower()))
        {
            return BadRequest($"ReportType must be one of: {string.Join(", ", validReportTypes)}");
        }

        var validFormats = new[] { "CSV", "PDF", "Excel" };
        if (!validFormats.Contains(request.Format.ToUpper()))
        {
            return BadRequest($"Format must be one of: {string.Join(", ", validFormats)}");
        }

        try
        {
            var report = await _analyticsService.GenerateExportableReportAsync(businessId, request, cancellationToken);
            return Ok(report);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Get campaign performance comparison across multiple campaigns
    /// </summary>
    [HttpPost("businesses/{businessId}/campaigns/compare")]
    [RequirePermission("analytics:read")]
    public async Task<ActionResult<List<DetailedCampaignPerformanceResponse>>> CompareCampaignPerformances(
        Guid businessId,
        [FromBody] List<Guid> campaignIds,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        if (campaignIds == null || !campaignIds.Any())
        {
            return BadRequest("At least one campaign ID is required");
        }

        if (campaignIds.Count > 10)
        {
            return BadRequest("Maximum 10 campaigns can be compared at once");
        }

        var results = new List<DetailedCampaignPerformanceResponse>();

        foreach (var campaignId in campaignIds)
        {
            try
            {
                var performance = await _analyticsService.GetDetailedCampaignPerformanceAsync(campaignId, startDate, endDate, cancellationToken);
                results.Add(performance);
            }
            catch (ArgumentException)
            {
                // Skip campaigns that don't exist or don't belong to this business
                continue;
            }
        }

        return Ok(results);
    }

    /// <summary>
    /// Get analytics summary dashboard data for a business
    /// </summary>
    [HttpGet("businesses/{businessId}/dashboard")]
    [RequirePermission("analytics:read")]
    public async Task<ActionResult<object>> GetAnalyticsDashboard(
        Guid businessId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var businessMetrics = await _analyticsService.GetBusinessMetricsAsync(businessId, startDate, endDate, cancellationToken);
            var playerEngagement = await _analyticsService.GetPlayerEngagementMetricsAsync(businessId, startDate, endDate, cancellationToken);
            var campaignPerformances = await _analyticsService.GetAllCampaignPerformancesAsync(businessId, startDate, endDate, cancellationToken);

            var dashboard = new
            {
                BusinessMetrics = businessMetrics,
                PlayerEngagement = playerEngagement,
                TopCampaigns = campaignPerformances
                    .OrderByDescending(cp => cp.Performance.TotalSpins)
                    .Take(5)
                    .ToList(),
                Summary = new
                {
                    TotalRevenue = businessMetrics.TotalTokenRevenue,
                    TotalPlayers = businessMetrics.UniquePlayers,
                    TotalCampaigns = campaignPerformances.Count,
                    AverageRedemptionRate = campaignPerformances.Any() 
                        ? campaignPerformances.Average(cp => cp.Performance.RedemptionRate) 
                        : 0m,
                    TrendData = await _analyticsService.GetDailyMetricsAsync(
                        businessId, 
                        startDate ?? DateTime.UtcNow.AddDays(-7), 
                        endDate ?? DateTime.UtcNow, 
                        cancellationToken)
                }
            };

            return Ok(dashboard);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    // Premium Analytics Endpoints (Premium+ subscription required)

    /// <summary>
    /// Get comprehensive premium analytics with advanced insights and predictions
    /// Requires Premium or Enterprise subscription
    /// </summary>
    [HttpGet("businesses/{businessId}/premium")]
    [RequirePermission("analytics:premium")]
    [RequireSubscriptionTier(SubscriptionTier.Premium)]
    public async Task<ActionResult<PremiumAnalyticsResponse>> GetPremiumAnalytics(
        Guid businessId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var premiumAnalytics = await _analyticsService.GetPremiumAnalyticsAsync(businessId, startDate, endDate, cancellationToken);
            return Ok(premiumAnalytics);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(402, new { error = "Premium subscription required", message = ex.Message });
        }
    }

    /// <summary>
    /// Get advanced campaign comparison analytics with statistical analysis
    /// Requires Premium or Enterprise subscription
    /// </summary>
    [HttpPost("businesses/{businessId}/campaigns/advanced-comparison")]
    [RequirePermission("analytics:premium")]
    [RequireSubscriptionTier(SubscriptionTier.Premium)]
    public async Task<ActionResult<AdvancedCampaignComparisonResponse>> GetAdvancedCampaignComparison(
        Guid businessId,
        [FromBody] List<Guid> campaignIds,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        if (campaignIds == null || !campaignIds.Any())
        {
            return BadRequest("At least one campaign ID is required");
        }

        if (campaignIds.Count > 20)
        {
            return BadRequest("Maximum 20 campaigns can be compared at once");
        }

        try
        {
            var comparison = await _analyticsService.GetAdvancedCampaignComparisonAsync(businessId, campaignIds, startDate, endDate, cancellationToken);
            return Ok(comparison);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(402, new { error = "Premium subscription required", message = ex.Message });
        }
    }

    /// <summary>
    /// Get AI-powered optimization recommendations for business performance
    /// Requires Premium or Enterprise subscription
    /// </summary>
    [HttpGet("businesses/{businessId}/optimization-recommendations")]
    [RequirePermission("analytics:premium")]
    [RequireSubscriptionTier(SubscriptionTier.Premium)]
    public async Task<ActionResult<List<OptimizationRecommendation>>> GetOptimizationRecommendations(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var recommendations = await _analyticsService.GetOptimizationRecommendationsAsync(businessId, cancellationToken);
            return Ok(recommendations);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(402, new { error = "Premium subscription required", message = ex.Message });
        }
    }

    /// <summary>
    /// Get predictive analytics with forecasting and trend analysis
    /// Requires Premium or Enterprise subscription
    /// </summary>
    [HttpGet("businesses/{businessId}/predictive-analytics")]
    [RequirePermission("analytics:premium")]
    [RequireSubscriptionTier(SubscriptionTier.Premium)]
    public async Task<ActionResult<PredictiveAnalytics>> GetPredictiveAnalytics(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var predictions = await _analyticsService.GetPredictiveAnalyticsAsync(businessId, cancellationToken);
            return Ok(predictions);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(402, new { error = "Premium subscription required", message = ex.Message });
        }
    }

    /// <summary>
    /// Get premium analytics feature availability for current subscription
    /// </summary>
    [HttpGet("businesses/{businessId}/premium/features")]
    [RequirePermission("analytics:read")]
    public async Task<ActionResult<object>> GetPremiumFeatureAvailability(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get business subscription tier from database
            var dbContext = HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();
            var business = await dbContext.Businesses.FindAsync(businessId);

            if (business == null)
            {
                return NotFound("Business not found");
            }

            var features = new
            {
                SubscriptionTier = business.SubscriptionTier.ToString(),
                AvailableFeatures = new
                {
                    BasicAnalytics = true,
                    AdvancedAnalytics = business.SubscriptionTier >= SubscriptionTier.Premium,
                    PredictiveAnalytics = business.SubscriptionTier >= SubscriptionTier.Premium,
                    CompetitiveBenchmarking = business.SubscriptionTier >= SubscriptionTier.Premium,
                    OptimizationRecommendations = business.SubscriptionTier >= SubscriptionTier.Premium,
                    AdvancedCampaignComparison = business.SubscriptionTier >= SubscriptionTier.Premium,
                    ExportableReports = business.SubscriptionTier >= SubscriptionTier.Premium,
                    RealTimeAlerts = business.SubscriptionTier >= SubscriptionTier.Enterprise,
                    CustomDashboards = business.SubscriptionTier >= SubscriptionTier.Enterprise,
                    APIAccess = business.SubscriptionTier >= SubscriptionTier.Enterprise
                },
                Limitations = business.SubscriptionTier switch
                {
                    SubscriptionTier.Basic => new
                    {
                        MaxCampaigns = 1,
                        HistoricalDataMonths = 3,
                        ExportFormats = new[] { "CSV" },
                        ReportFrequency = "Weekly"
                    },
                    SubscriptionTier.Premium => new
                    {
                        MaxCampaigns = 5,
                        HistoricalDataMonths = 12,
                        ExportFormats = new[] { "CSV", "PDF", "Excel" },
                        ReportFrequency = "Daily"
                    },
                    SubscriptionTier.Enterprise => new
                    {
                        MaxCampaigns = int.MaxValue,
                        HistoricalDataMonths = int.MaxValue,
                        ExportFormats = new[] { "CSV", "PDF", "Excel", "JSON", "XML" },
                        ReportFrequency = "Real-time"
                    },
                    _ => new
                    {
                        MaxCampaigns = 0,
                        HistoricalDataMonths = 0,
                        ExportFormats = new string[] { },
                        ReportFrequency = "None"
                    }
                },
                UpgradeOptions = business.SubscriptionTier < SubscriptionTier.Enterprise ? new
                {
                    NextTier = business.SubscriptionTier == SubscriptionTier.Basic ? "Premium" : "Enterprise",
                    AdditionalFeatures = business.SubscriptionTier == SubscriptionTier.Basic 
                        ? new[] { "Predictive Analytics", "Competitive Benchmarking", "Advanced Reports" }
                        : new[] { "Real-time Alerts", "Custom Dashboards", "API Access" },
                    EstimatedMonthlyCost = business.SubscriptionTier == SubscriptionTier.Basic ? 49.99m : 149.99m
                } : null
            };

            return Ok(features);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }
}