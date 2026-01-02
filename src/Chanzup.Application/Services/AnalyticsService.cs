using Microsoft.EntityFrameworkCore;
using Chanzup.Application.Interfaces;
using Chanzup.Application.DTOs;
using Chanzup.Domain.Entities;
using Chanzup.Domain.ValueObjects;
using System.Text.Json;

namespace Chanzup.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IApplicationDbContext _context;

    public AnalyticsService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task TrackQRScanAsync(Guid businessId, Guid playerId, Guid campaignId, int tokensEarned, CancellationToken cancellationToken = default)
    {
        var analyticsEvent = AnalyticsEvent.CreateQRScanEvent(businessId, playerId, campaignId, tokensEarned);
        _context.AnalyticsEvents.Add(analyticsEvent);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task TrackWheelSpinAsync(Guid businessId, Guid playerId, Guid campaignId, int tokensSpent, bool wonPrize, Guid? prizeId = null, CancellationToken cancellationToken = default)
    {
        var analyticsEvent = AnalyticsEvent.CreateWheelSpinEvent(businessId, playerId, campaignId, tokensSpent, wonPrize, prizeId);
        _context.AnalyticsEvents.Add(analyticsEvent);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task TrackPrizeRedemptionAsync(Guid businessId, Guid playerId, Guid prizeId, CancellationToken cancellationToken = default)
    {
        var analyticsEvent = AnalyticsEvent.CreatePrizeRedemptionEvent(businessId, playerId, prizeId);
        _context.AnalyticsEvents.Add(analyticsEvent);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task TrackPlayerRegistrationAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var analyticsEvent = AnalyticsEvent.CreatePlayerRegistrationEvent(playerId);
        _context.AnalyticsEvents.Add(analyticsEvent);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task TrackCampaignCreatedAsync(Guid businessId, Guid campaignId, CancellationToken cancellationToken = default)
    {
        var analyticsEvent = AnalyticsEvent.CreateCampaignCreatedEvent(businessId, campaignId);
        _context.AnalyticsEvents.Add(analyticsEvent);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task TrackCustomEventAsync(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default)
    {
        _context.AnalyticsEvents.Add(analyticsEvent);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<CampaignMetrics> GetCampaignMetricsAsync(Guid campaignId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var campaign = await _context.Campaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);

        if (campaign == null)
            throw new ArgumentException($"Campaign with ID {campaignId} not found");

        // Get wheel spins for the campaign in the date range
        var wheelSpins = await _context.WheelSpins
            .Where(ws => ws.CampaignId == campaignId && ws.CreatedAt >= start && ws.CreatedAt <= end)
            .ToListAsync(cancellationToken);

        // Get prize redemptions for prizes from this campaign
        var prizeIds = await _context.Prizes
            .Where(p => p.CampaignId == campaignId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var redemptions = await _context.PlayerPrizes
            .Where(pp => prizeIds.Contains(pp.PrizeId) && pp.IsRedeemed && pp.RedeemedAt >= start && pp.RedeemedAt <= end)
            .CountAsync(cancellationToken);

        var totalSpins = wheelSpins.Count;
        var uniquePlayers = wheelSpins.Select(ws => ws.PlayerId).Distinct().Count();
        var prizesAwarded = wheelSpins.Count(ws => ws.PrizeId.HasValue);
        var tokenRevenue = wheelSpins.Sum(ws => ws.TokensSpent);

        return new CampaignMetrics
        {
            CampaignId = campaignId,
            CampaignName = campaign.Name,
            TotalSpins = totalSpins,
            UniquePlayers = uniquePlayers,
            PrizesAwarded = prizesAwarded,
            PrizesRedeemed = redemptions,
            RedemptionRate = prizesAwarded > 0 ? (decimal)redemptions / prizesAwarded : 0,
            TokenRevenue = tokenRevenue,
            AverageSpinsPerPlayer = uniquePlayers > 0 ? (double)totalSpins / uniquePlayers : 0,
            PeriodStart = start,
            PeriodEnd = end
        };
    }

    public async Task<BusinessMetrics> GetBusinessMetricsAsync(Guid businessId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var business = await _context.Businesses
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

        if (business == null)
            throw new ArgumentException($"Business with ID {businessId} not found");

        // Get QR scans for this business
        var qrScans = await _context.QRSessions
            .Where(qr => qr.BusinessId == businessId && qr.CreatedAt >= start && qr.CreatedAt <= end)
            .ToListAsync(cancellationToken);

        // Get wheel spins for campaigns belonging to this business
        var campaignIds = await _context.Campaigns
            .Where(c => c.BusinessId == businessId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var wheelSpins = await _context.WheelSpins
            .Where(ws => campaignIds.Contains(ws.CampaignId) && ws.CreatedAt >= start && ws.CreatedAt <= end)
            .ToListAsync(cancellationToken);

        // Get prize redemptions for prizes from this business's campaigns
        var prizeIds = await _context.Prizes
            .Where(p => campaignIds.Contains(p.CampaignId))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var redemptions = await _context.PlayerPrizes
            .Where(pp => prizeIds.Contains(pp.PrizeId) && pp.IsRedeemed && pp.RedeemedAt >= start && pp.RedeemedAt <= end)
            .CountAsync(cancellationToken);

        var activeCampaigns = await _context.Campaigns
            .Where(c => c.BusinessId == businessId && c.IsActive)
            .CountAsync(cancellationToken);

        var allPlayerIds = qrScans.Select(qr => qr.PlayerId)
            .Union(wheelSpins.Select(ws => ws.PlayerId))
            .Distinct();

        var totalSpins = wheelSpins.Count;
        var prizesAwarded = wheelSpins.Count(ws => ws.PrizeId.HasValue);

        return new BusinessMetrics
        {
            BusinessId = businessId,
            BusinessName = business.Name,
            TotalQRScans = qrScans.Count,
            TotalSpins = totalSpins,
            UniquePlayers = allPlayerIds.Count(),
            TotalPrizesAwarded = prizesAwarded,
            TotalPrizesRedeemed = redemptions,
            OverallRedemptionRate = prizesAwarded > 0 ? (decimal)redemptions / prizesAwarded : 0,
            TotalTokenRevenue = wheelSpins.Sum(ws => ws.TokensSpent),
            ActiveCampaigns = activeCampaigns,
            PeriodStart = start,
            PeriodEnd = end
        };
    }

    public async Task<PlayerEngagementMetrics> GetPlayerEngagementMetricsAsync(Guid businessId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;
        var now = DateTime.UtcNow;

        // Get all player interactions with this business
        var qrScans = await _context.QRSessions
            .Where(qr => qr.BusinessId == businessId && qr.CreatedAt >= start && qr.CreatedAt <= end)
            .ToListAsync(cancellationToken);

        var campaignIds = await _context.Campaigns
            .Where(c => c.BusinessId == businessId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var wheelSpins = await _context.WheelSpins
            .Where(ws => campaignIds.Contains(ws.CampaignId) && ws.CreatedAt >= start && ws.CreatedAt <= end)
            .ToListAsync(cancellationToken);

        var allPlayerIds = qrScans.Select(qr => qr.PlayerId)
            .Union(wheelSpins.Select(ws => ws.PlayerId))
            .Distinct()
            .ToList();

        // Calculate active players for different periods
        var activePlayersToday = qrScans.Where(qr => qr.CreatedAt.Date == now.Date)
            .Select(qr => qr.PlayerId)
            .Union(wheelSpins.Where(ws => ws.CreatedAt.Date == now.Date).Select(ws => ws.PlayerId))
            .Distinct()
            .Count();

        var weekStart = now.AddDays(-(int)now.DayOfWeek);
        var activePlayersThisWeek = qrScans.Where(qr => qr.CreatedAt >= weekStart)
            .Select(qr => qr.PlayerId)
            .Union(wheelSpins.Where(ws => ws.CreatedAt >= weekStart).Select(ws => ws.PlayerId))
            .Distinct()
            .Count();

        var monthStart = new DateTime(now.Year, now.Month, 1);
        var activePlayersThisMonth = qrScans.Where(qr => qr.CreatedAt >= monthStart)
            .Select(qr => qr.PlayerId)
            .Union(wheelSpins.Where(ws => ws.CreatedAt >= monthStart).Select(ws => ws.PlayerId))
            .Distinct()
            .Count();

        var totalPlayers = allPlayerIds.Count;
        var totalSessions = qrScans.Count;
        var totalSpins = wheelSpins.Count;

        return new PlayerEngagementMetrics
        {
            BusinessId = businessId,
            TotalPlayers = totalPlayers,
            ActivePlayersToday = activePlayersToday,
            ActivePlayersThisWeek = activePlayersThisWeek,
            ActivePlayersThisMonth = activePlayersThisMonth,
            AverageSessionsPerPlayer = totalPlayers > 0 ? (double)totalSessions / totalPlayers : 0,
            AverageSpinsPerPlayer = totalPlayers > 0 ? (double)totalSpins / totalPlayers : 0,
            PeriodStart = start,
            PeriodEnd = end
        };
    }

    public async Task<List<DailyMetrics>> GetDailyMetricsAsync(Guid businessId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var campaignIds = await _context.Campaigns
            .Where(c => c.BusinessId == businessId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var qrScans = await _context.QRSessions
            .Where(qr => qr.BusinessId == businessId && qr.CreatedAt >= startDate && qr.CreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        var wheelSpins = await _context.WheelSpins
            .Where(ws => campaignIds.Contains(ws.CampaignId) && ws.CreatedAt >= startDate && ws.CreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        var prizeIds = await _context.Prizes
            .Where(p => campaignIds.Contains(p.CampaignId))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var redemptions = await _context.PlayerPrizes
            .Where(pp => prizeIds.Contains(pp.PrizeId) && pp.IsRedeemed && pp.RedeemedAt >= startDate && pp.RedeemedAt <= endDate)
            .ToListAsync(cancellationToken);

        var dailyMetrics = new List<DailyMetrics>();
        
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var dayQrScans = qrScans.Where(qr => qr.CreatedAt.Date == date).ToList();
            var dayWheelSpins = wheelSpins.Where(ws => ws.CreatedAt.Date == date).ToList();
            var dayRedemptions = redemptions.Where(r => r.RedeemedAt?.Date == date).ToList();

            var uniquePlayers = dayQrScans.Select(qr => qr.PlayerId)
                .Union(dayWheelSpins.Select(ws => ws.PlayerId))
                .Distinct()
                .Count();

            dailyMetrics.Add(new DailyMetrics
            {
                Date = date,
                QRScans = dayQrScans.Count,
                Spins = dayWheelSpins.Count,
                UniquePlayers = uniquePlayers,
                PrizesAwarded = dayWheelSpins.Count(ws => ws.PrizeId.HasValue),
                PrizesRedeemed = dayRedemptions.Count,
                TokensEarned = dayQrScans.Sum(qr => qr.TokensEarned),
                TokensSpent = dayWheelSpins.Sum(ws => ws.TokensSpent)
            });
        }

        return dailyMetrics;
    }

    public async Task<DetailedCampaignPerformanceResponse> GetDetailedCampaignPerformanceAsync(Guid campaignId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var campaign = await _context.Campaigns
            .Include(c => c.Prizes)
            .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);

        if (campaign == null)
            throw new ArgumentException($"Campaign with ID {campaignId} not found");

        // Get all wheel spins for the campaign
        var wheelSpins = await _context.WheelSpins
            .Where(ws => ws.CampaignId == campaignId && ws.CreatedAt >= start && ws.CreatedAt <= end)
            .ToListAsync(cancellationToken);

        // Get QR sessions that led to this campaign
        var qrSessions = await _context.QRSessions
            .Where(qr => qr.BusinessId == campaign.BusinessId && qr.CreatedAt >= start && qr.CreatedAt <= end)
            .ToListAsync(cancellationToken);

        // Get prize redemptions
        var prizeIds = campaign.Prizes.Select(p => p.Id).ToList();
        var redemptions = await _context.PlayerPrizes
            .Where(pp => prizeIds.Contains(pp.PrizeId) && pp.IsRedeemed && pp.RedeemedAt >= start && pp.RedeemedAt <= end)
            .ToListAsync(cancellationToken);

        // Calculate performance metrics
        var performance = CalculateCampaignPerformanceMetrics(wheelSpins, qrSessions, redemptions);

        // Calculate prize performance
        var prizePerformance = CalculatePrizePerformanceMetrics(campaign.Prizes, wheelSpins, redemptions);

        // Calculate hourly breakdown
        var hourlyBreakdown = CalculateHourlyMetrics(wheelSpins);

        // Calculate daily breakdown
        var dailyBreakdown = CalculateDailyMetricsForCampaign(wheelSpins, qrSessions, redemptions, start, end);

        // Calculate player segmentation
        var playerSegmentation = CalculatePlayerSegmentationMetrics(wheelSpins, redemptions);

        // Calculate conversion funnel
        var conversionFunnel = CalculateConversionFunnelMetrics(qrSessions, wheelSpins, redemptions);

        return new DetailedCampaignPerformanceResponse
        {
            CampaignId = campaign.Id,
            CampaignName = campaign.Name,
            Description = campaign.Description ?? string.Empty,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            IsActive = campaign.IsActive,
            Performance = performance,
            PrizePerformance = prizePerformance,
            HourlyBreakdown = hourlyBreakdown,
            DailyBreakdown = dailyBreakdown,
            PlayerSegmentation = playerSegmentation,
            ConversionFunnel = conversionFunnel
        };
    }

    public async Task<ComprehensivePlayerBehaviorResponse> GetComprehensivePlayerBehaviorAsync(Guid businessId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var business = await _context.Businesses
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

        if (business == null)
            throw new ArgumentException($"Business with ID {businessId} not found");

        // Get all player interactions with this business
        var qrSessions = await _context.QRSessions
            .Where(qr => qr.BusinessId == businessId && qr.CreatedAt >= start && qr.CreatedAt <= end)
            .ToListAsync(cancellationToken);

        var campaignIds = await _context.Campaigns
            .Where(c => c.BusinessId == businessId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var wheelSpins = await _context.WheelSpins
            .Where(ws => campaignIds.Contains(ws.CampaignId) && ws.CreatedAt >= start && ws.CreatedAt <= end)
            .ToListAsync(cancellationToken);

        var prizeIds = await _context.Prizes
            .Where(p => campaignIds.Contains(p.CampaignId))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var redemptions = await _context.PlayerPrizes
            .Where(pp => prizeIds.Contains(pp.PrizeId) && pp.RedeemedAt >= start && pp.RedeemedAt <= end)
            .ToListAsync(cancellationToken);

        var allPlayerIds = qrSessions.Select(qr => qr.PlayerId)
            .Union(wheelSpins.Select(ws => ws.PlayerId))
            .Distinct()
            .ToList();

        var players = await _context.Players
            .Where(p => allPlayerIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        // Calculate demographics
        var demographics = CalculatePlayerDemographicsMetrics(players, qrSessions, wheelSpins);

        // Calculate engagement patterns
        var engagementPatterns = CalculatePlayerEngagementPatterns(qrSessions, wheelSpins);

        // Calculate behavioral insights
        var behavioralInsights = await CalculatePlayerBehavioralInsights(players, wheelSpins, redemptions, prizeIds, cancellationToken);

        // Calculate retention metrics
        var retention = await CalculatePlayerRetentionMetrics(businessId, players, start, end, cancellationToken);

        // Calculate top players
        var topPlayers = CalculateTopPlayerMetrics(players, wheelSpins, redemptions);

        return new ComprehensivePlayerBehaviorResponse
        {
            BusinessId = businessId,
            BusinessName = business.Name,
            PeriodStart = start,
            PeriodEnd = end,
            Demographics = demographics,
            EngagementPatterns = engagementPatterns,
            BehavioralInsights = behavioralInsights,
            Retention = retention,
            GeographicBreakdown = new List<GeographicMetrics>(), // Would need location data
            TopPlayers = topPlayers
        };
    }

    public async Task<ExportableReportResponse> GenerateExportableReportAsync(Guid businessId, ReportGenerationRequest request, CancellationToken cancellationToken = default)
    {
        var reportId = Guid.NewGuid().ToString();
        var business = await _context.Businesses
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

        if (business == null)
            throw new ArgumentException($"Business with ID {businessId} not found");

        // Generate report based on type
        var reportData = request.ReportType.ToLower() switch
        {
            "campaign" => await GenerateCampaignReport(businessId, request, cancellationToken),
            "business" => await GenerateBusinessReport(businessId, request, cancellationToken),
            "player-behavior" => await GeneratePlayerBehaviorReport(businessId, request, cancellationToken),
            _ => throw new ArgumentException($"Unsupported report type: {request.ReportType}")
        };

        // In a real implementation, you would:
        // 1. Generate the actual file (CSV, PDF, Excel)
        // 2. Store it in blob storage
        // 3. Return a download URL
        // For now, we'll return a mock response

        var downloadUrl = $"https://api.chanzup.com/reports/{reportId}/download";
        var expiresAt = DateTime.UtcNow.AddHours(24);

        return new ExportableReportResponse
        {
            ReportId = reportId,
            ReportType = request.ReportType,
            Format = request.Format,
            GeneratedAt = DateTime.UtcNow,
            DownloadUrl = downloadUrl,
            ExpiresAt = expiresAt,
            Metadata = new ReportMetadata
            {
                BusinessId = businessId,
                BusinessName = business.Name,
                PeriodStart = request.StartDate,
                PeriodEnd = request.EndDate,
                TotalRecords = GetReportRecordCount(reportData),
                IncludedMetrics = request.IncludeMetrics,
                Parameters = request.Filters
            }
        };
    }

    public async Task<List<DetailedCampaignPerformanceResponse>> GetAllCampaignPerformancesAsync(Guid businessId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var campaignIds = await _context.Campaigns
            .Where(c => c.BusinessId == businessId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var results = new List<DetailedCampaignPerformanceResponse>();

        foreach (var campaignId in campaignIds)
        {
            try
            {
                var performance = await GetDetailedCampaignPerformanceAsync(campaignId, startDate, endDate, cancellationToken);
                results.Add(performance);
            }
            catch (ArgumentException)
            {
                // Skip campaigns that no longer exist
                continue;
            }
        }

        return results;
    }

    // Helper methods for calculations
    private CampaignPerformanceMetrics CalculateCampaignPerformanceMetrics(
        List<WheelSpin> wheelSpins, 
        List<QRSession> qrSessions, 
        List<PlayerPrize> redemptions)
    {
        var uniquePlayers = wheelSpins.Select(ws => ws.PlayerId).Distinct().ToList();
        var newPlayers = qrSessions.Where(qr => qr.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .Select(qr => qr.PlayerId).Distinct().Count();
        var returningPlayers = uniquePlayers.Count - newPlayers;

        var prizesAwarded = wheelSpins.Count(ws => ws.PrizeId.HasValue);
        var prizesRedeemed = redemptions.Count(r => r.IsRedeemed);

        var sessionGroups = qrSessions.GroupBy(qr => new { qr.PlayerId, Date = qr.CreatedAt.Date });
        var totalSessions = sessionGroups.Count();

        return new CampaignPerformanceMetrics
        {
            TotalSpins = wheelSpins.Count,
            UniquePlayers = uniquePlayers.Count,
            NewPlayers = newPlayers,
            ReturningPlayers = returningPlayers,
            PrizesAwarded = prizesAwarded,
            PrizesRedeemed = prizesRedeemed,
            RedemptionRate = prizesAwarded > 0 ? (decimal)prizesRedeemed / prizesAwarded : 0,
            TokenRevenue = wheelSpins.Sum(ws => ws.TokensSpent),
            AverageSpinsPerPlayer = uniquePlayers.Count > 0 ? (double)wheelSpins.Count / uniquePlayers.Count : 0,
            AverageSpinsPerSession = totalSessions > 0 ? (double)wheelSpins.Count / totalSessions : 0,
            WinRate = wheelSpins.Count > 0 ? (decimal)prizesAwarded / wheelSpins.Count : 0,
            AveragePrizeValue = 0, // Would need prize value data
            AverageSessionDuration = TimeSpan.FromMinutes(15), // Mock value
            TotalSessions = totalSessions,
            SessionsPerPlayer = uniquePlayers.Count > 0 ? (double)totalSessions / uniquePlayers.Count : 0
        };
    }

    private List<PrizePerformanceMetrics> CalculatePrizePerformanceMetrics(
        ICollection<Prize> prizes, 
        List<WheelSpin> wheelSpins, 
        List<PlayerPrize> redemptions)
    {
        return prizes.Select(prize =>
        {
            var timesWon = wheelSpins.Count(ws => ws.PrizeId == prize.Id);
            var timesRedeemed = redemptions.Count(r => r.PrizeId == prize.Id && r.IsRedeemed);
            var totalSpins = wheelSpins.Count;

            return new PrizePerformanceMetrics
            {
                PrizeId = prize.Id,
                PrizeName = prize.Name,
                PrizeValue = prize.Value?.Amount ?? 0,
                TotalQuantity = prize.TotalQuantity,
                RemainingQuantity = prize.RemainingQuantity,
                TimesWon = timesWon,
                TimesRedeemed = timesRedeemed,
                WinRate = totalSpins > 0 ? (decimal)timesWon / totalSpins : 0,
                RedemptionRate = timesWon > 0 ? (decimal)timesRedeemed / timesWon : 0,
                ConfiguredProbability = prize.WinProbability,
                ActualWinProbability = totalSpins > 0 ? (decimal)timesWon / totalSpins : 0,
                PopularityScore = timesWon * 0.7m + timesRedeemed * 0.3m
            };
        }).ToList();
    }

    private List<HourlyMetrics> CalculateHourlyMetrics(List<WheelSpin> wheelSpins)
    {
        return wheelSpins
            .GroupBy(ws => ws.CreatedAt.Hour)
            .Select(g => new HourlyMetrics
            {
                Hour = g.Key,
                Spins = g.Count(),
                UniquePlayers = g.Select(ws => ws.PlayerId).Distinct().Count(),
                PrizesAwarded = g.Count(ws => ws.PrizeId.HasValue),
                TokensSpent = g.Sum(ws => ws.TokensSpent)
            })
            .OrderBy(hm => hm.Hour)
            .ToList();
    }

    private List<DailyMetrics> CalculateDailyMetricsForCampaign(
        List<WheelSpin> wheelSpins, 
        List<QRSession> qrSessions, 
        List<PlayerPrize> redemptions, 
        DateTime start, 
        DateTime end)
    {
        var dailyMetrics = new List<DailyMetrics>();
        
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            var daySpins = wheelSpins.Where(ws => ws.CreatedAt.Date == date).ToList();
            var dayScans = qrSessions.Where(qr => qr.CreatedAt.Date == date).ToList();
            var dayRedemptions = redemptions.Where(r => r.RedeemedAt?.Date == date).ToList();

            var uniquePlayers = daySpins.Select(ws => ws.PlayerId)
                .Union(dayScans.Select(qr => qr.PlayerId))
                .Distinct()
                .Count();

            dailyMetrics.Add(new DailyMetrics
            {
                Date = date,
                QRScans = dayScans.Count,
                Spins = daySpins.Count,
                UniquePlayers = uniquePlayers,
                PrizesAwarded = daySpins.Count(ws => ws.PrizeId.HasValue),
                PrizesRedeemed = dayRedemptions.Count,
                TokensEarned = dayScans.Sum(qr => qr.TokensEarned),
                TokensSpent = daySpins.Sum(ws => ws.TokensSpent)
            });
        }

        return dailyMetrics;
    }

    private PlayerSegmentationMetrics CalculatePlayerSegmentationMetrics(
        List<WheelSpin> wheelSpins, 
        List<PlayerPrize> redemptions)
    {
        var playerSpinCounts = wheelSpins
            .GroupBy(ws => ws.PlayerId)
            .ToDictionary(g => g.Key, g => g.Count());

        var lowEngagement = playerSpinCounts.Where(kvp => kvp.Value <= 2).ToList();
        var mediumEngagement = playerSpinCounts.Where(kvp => kvp.Value >= 3 && kvp.Value <= 10).ToList();
        var highEngagement = playerSpinCounts.Where(kvp => kvp.Value >= 11).ToList();

        return new PlayerSegmentationMetrics
        {
            LowEngagementPlayers = lowEngagement.Count,
            MediumEngagementPlayers = mediumEngagement.Count,
            HighEngagementPlayers = highEngagement.Count,
            AverageSpinsLowEngagement = lowEngagement.Count > 0 ? lowEngagement.Average(le => le.Value) : 0,
            AverageSpinsMediumEngagement = mediumEngagement.Count > 0 ? mediumEngagement.Average(me => me.Value) : 0,
            AverageSpinsHighEngagement = highEngagement.Count > 0 ? highEngagement.Average(he => he.Value) : 0,
            RedemptionRateLowEngagement = CalculateRedemptionRateForSegment(lowEngagement.Select(le => le.Key), wheelSpins, redemptions),
            RedemptionRateMediumEngagement = CalculateRedemptionRateForSegment(mediumEngagement.Select(me => me.Key), wheelSpins, redemptions),
            RedemptionRateHighEngagement = CalculateRedemptionRateForSegment(highEngagement.Select(he => he.Key), wheelSpins, redemptions)
        };
    }

    private decimal CalculateRedemptionRateForSegment(
        IEnumerable<Guid> playerIds, 
        List<WheelSpin> wheelSpins, 
        List<PlayerPrize> redemptions)
    {
        var segmentSpins = wheelSpins.Where(ws => playerIds.Contains(ws.PlayerId)).ToList();
        var segmentPrizes = segmentSpins.Count(ws => ws.PrizeId.HasValue);
        var segmentRedemptions = redemptions.Count(r => playerIds.Contains(r.PlayerId) && r.IsRedeemed);

        return segmentPrizes > 0 ? (decimal)segmentRedemptions / segmentPrizes : 0;
    }

    private ConversionFunnelMetrics CalculateConversionFunnelMetrics(
        List<QRSession> qrSessions, 
        List<WheelSpin> wheelSpins, 
        List<PlayerPrize> redemptions)
    {
        var qrScans = qrSessions.Count;
        var playersWhoSpun = wheelSpins.Select(ws => ws.PlayerId).Distinct().Count();
        var playersWhoWonPrizes = wheelSpins.Where(ws => ws.PrizeId.HasValue).Select(ws => ws.PlayerId).Distinct().Count();
        var playersWhoRedeemed = redemptions.Where(r => r.IsRedeemed).Select(r => r.PlayerId).Distinct().Count();

        return new ConversionFunnelMetrics
        {
            QRScans = qrScans,
            PlayersWhoSpun = playersWhoSpun,
            PlayersWhoWonPrizes = playersWhoWonPrizes,
            PlayersWhoRedeemed = playersWhoRedeemed,
            ScanToSpinConversion = qrScans > 0 ? (decimal)playersWhoSpun / qrScans : 0,
            SpinToWinConversion = playersWhoSpun > 0 ? (decimal)playersWhoWonPrizes / playersWhoSpun : 0,
            WinToRedemptionConversion = playersWhoWonPrizes > 0 ? (decimal)playersWhoRedeemed / playersWhoWonPrizes : 0,
            OverallConversion = qrScans > 0 ? (decimal)playersWhoRedeemed / qrScans : 0
        };
    }

    private PlayerDemographicsMetrics CalculatePlayerDemographicsMetrics(
        List<Player> players, 
        List<QRSession> qrSessions, 
        List<WheelSpin> wheelSpins)
    {
        var now = DateTime.UtcNow;
        var newPlayersThisPeriod = players.Count(p => p.CreatedAt >= now.AddDays(-30));

        var activeToday = qrSessions.Where(qr => qr.CreatedAt.Date == now.Date)
            .Select(qr => qr.PlayerId)
            .Union(wheelSpins.Where(ws => ws.CreatedAt.Date == now.Date).Select(ws => ws.PlayerId))
            .Distinct()
            .Count();

        var weekStart = now.AddDays(-(int)now.DayOfWeek);
        var activeThisWeek = qrSessions.Where(qr => qr.CreatedAt >= weekStart)
            .Select(qr => qr.PlayerId)
            .Union(wheelSpins.Where(ws => ws.CreatedAt >= weekStart).Select(ws => ws.PlayerId))
            .Distinct()
            .Count();

        var monthStart = new DateTime(now.Year, now.Month, 1);
        var activeThisMonth = qrSessions.Where(qr => qr.CreatedAt >= monthStart)
            .Select(qr => qr.PlayerId)
            .Union(wheelSpins.Where(ws => ws.CreatedAt >= monthStart).Select(ws => ws.PlayerId))
            .Distinct()
            .Count();

        return new PlayerDemographicsMetrics
        {
            TotalPlayers = players.Count,
            NewPlayersThisPeriod = newPlayersThisPeriod,
            ActivePlayersToday = activeToday,
            ActivePlayersThisWeek = activeThisWeek,
            ActivePlayersThisMonth = activeThisMonth,
            AveragePlayerAge = 0, // Would need birth date data
            PlayersByRegistrationSource = new Dictionary<string, int> { { "Direct", players.Count } }
        };
    }

    private PlayerEngagementPatterns CalculatePlayerEngagementPatterns(
        List<QRSession> qrSessions, 
        List<WheelSpin> wheelSpins)
    {
        var allActivities = qrSessions.Select(qr => qr.CreatedAt)
            .Union(wheelSpins.Select(ws => ws.CreatedAt))
            .ToList();

        var spinsByHour = allActivities
            .GroupBy(dt => dt.Hour)
            .ToDictionary(g => g.Key, g => g.Count());

        var spinsByDayOfWeek = allActivities
            .GroupBy(dt => dt.DayOfWeek.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var peakHour = spinsByHour.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key;
        var peakDay = spinsByDayOfWeek.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? "Monday";

        return new PlayerEngagementPatterns
        {
            SpinsByHour = spinsByHour,
            SpinsByDayOfWeek = spinsByDayOfWeek,
            AverageSessionDuration = 15.0, // Mock value in minutes
            AverageSpinsPerSession = wheelSpins.Count > 0 ? (double)wheelSpins.Count / Math.Max(qrSessions.Count, 1) : 0,
            AverageTimeBetweenSessions = 24.0, // Mock value in hours
            PeakHour = peakHour,
            PeakDay = peakDay
        };
    }

    private async Task<PlayerBehavioralInsights> CalculatePlayerBehavioralInsights(
        List<Player> players, 
        List<WheelSpin> wheelSpins, 
        List<PlayerPrize> redemptions, 
        List<Guid> prizeIds,
        CancellationToken cancellationToken)
    {
        var averageTokenBalance = players.Count > 0 ? players.Average(p => p.TokenBalance) : 0;
        var playersWithZeroBalance = players.Count(p => p.TokenBalance == 0);
        var playersWithHighBalance = players.Count(p => p.TokenBalance > 50);

        var churnRate = await CalculateChurnRate(players, cancellationToken);

        var prizeNames = await _context.Prizes
            .Where(p => prizeIds.Contains(p.Id))
            .Select(p => p.Name)
            .ToListAsync(cancellationToken);

        return new PlayerBehavioralInsights
        {
            AverageTokenBalance = (decimal)averageTokenBalance,
            PlayersWithZeroBalance = playersWithZeroBalance,
            PlayersWithHighBalance = playersWithHighBalance,
            AverageSpinsBeforeFirstWin = 3.5, // Mock calculation
            AverageTimeBetweenWinAndRedemption = 2.5, // Mock value in hours
            ChurnRate = churnRate,
            MostPopularPrizes = prizeNames.Take(5).ToList(),
            LeastPopularPrizes = prizeNames.TakeLast(3).ToList()
        };
    }

    private async Task<decimal> CalculateChurnRate(List<Player> players, CancellationToken cancellationToken)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var inactivePlayers = 0;

        foreach (var player in players)
        {
            var hasRecentActivity = await _context.QRSessions
                .AnyAsync(qr => qr.PlayerId == player.Id && qr.CreatedAt >= thirtyDaysAgo, cancellationToken) ||
                await _context.WheelSpins
                .AnyAsync(ws => ws.PlayerId == player.Id && ws.CreatedAt >= thirtyDaysAgo, cancellationToken);

            if (!hasRecentActivity)
                inactivePlayers++;
        }

        return players.Count > 0 ? (decimal)inactivePlayers / players.Count : 0;
    }

    private async Task<PlayerRetentionMetrics> CalculatePlayerRetentionMetrics(
        Guid businessId, 
        List<Player> players, 
        DateTime start, 
        DateTime end, 
        CancellationToken cancellationToken)
    {
        // This is a simplified calculation - in reality, you'd need more sophisticated cohort analysis
        var monthlyChurnRate = await CalculateChurnRate(players, cancellationToken);
        
        return new PlayerRetentionMetrics
        {
            Day1Retention = 0.85m, // Mock values - would need proper cohort analysis
            Day7Retention = 0.65m,
            Day30Retention = 0.35m,
            MonthlyChurnRate = monthlyChurnRate,
            AveragePlayerLifetime = 45.0, // Mock value in days
            ReturningPlayersThisMonth = players.Count(p => p.CreatedAt < start),
            LapsedPlayersReactivated = 0 // Would need to track reactivation
        };
    }

    private List<TopPlayerMetrics> CalculateTopPlayerMetrics(
        List<Player> players, 
        List<WheelSpin> wheelSpins, 
        List<PlayerPrize> redemptions)
    {
        var playerMetrics = players.Select(player =>
        {
            var playerSpins = wheelSpins.Where(ws => ws.PlayerId == player.Id).ToList();
            var playerRedemptions = redemptions.Where(r => r.PlayerId == player.Id).ToList();

            return new TopPlayerMetrics
            {
                PlayerId = player.Id,
                PlayerName = $"Player {player.Id.ToString()[..8]}", // Anonymized
                TotalSpins = playerSpins.Count,
                PrizesWon = playerSpins.Count(ws => ws.PrizeId.HasValue),
                PrizesRedeemed = playerRedemptions.Count(r => r.IsRedeemed),
                TokensSpent = playerSpins.Sum(ws => ws.TokensSpent),
                LastActivity = playerSpins.Any() ? playerSpins.Max(ws => ws.CreatedAt) : player.CreatedAt,
                DaysActive = (DateTime.UtcNow - player.CreatedAt).Days
            };
        })
        .OrderByDescending(pm => pm.TotalSpins)
        .Take(10)
        .ToList();

        return playerMetrics;
    }

    private async Task<object> GenerateCampaignReport(Guid businessId, ReportGenerationRequest request, CancellationToken cancellationToken)
    {
        var campaigns = await GetAllCampaignPerformancesAsync(businessId, request.StartDate, request.EndDate, cancellationToken);
        return campaigns;
    }

    private async Task<object> GenerateBusinessReport(Guid businessId, ReportGenerationRequest request, CancellationToken cancellationToken)
    {
        var businessMetrics = await GetBusinessMetricsAsync(businessId, request.StartDate, request.EndDate, cancellationToken);
        return businessMetrics;
    }

    private async Task<object> GeneratePlayerBehaviorReport(Guid businessId, ReportGenerationRequest request, CancellationToken cancellationToken)
    {
        var playerBehavior = await GetComprehensivePlayerBehaviorAsync(businessId, request.StartDate, request.EndDate, cancellationToken);
        return playerBehavior;
    }

    private int GetReportRecordCount(object reportData)
    {
        return reportData switch
        {
            List<DetailedCampaignPerformanceResponse> campaigns => campaigns.Count,
            BusinessMetrics => 1,
            ComprehensivePlayerBehaviorResponse => 1,
            _ => 0
        };
    }

    // Premium Analytics Methods (Premium+ subscription required)
    
    public async Task<PremiumAnalyticsResponse> GetPremiumAnalyticsAsync(Guid businessId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var business = await _context.Businesses
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

        if (business == null)
            throw new ArgumentException($"Business with ID {businessId} not found");

        // Verify subscription tier
        if (business.SubscriptionTier < SubscriptionTier.Premium)
            throw new UnauthorizedAccessException("Premium subscription required for advanced analytics");

        // Get comprehensive data for analysis
        var businessMetrics = await GetBusinessMetricsAsync(businessId, start, end, cancellationToken);
        var playerBehavior = await GetComprehensivePlayerBehaviorAsync(businessId, start, end, cancellationToken);
        var campaignPerformances = await GetAllCampaignPerformancesAsync(businessId, start, end, cancellationToken);

        // Calculate advanced performance metrics
        var advancedPerformance = await CalculateAdvancedPerformanceMetrics(businessId, businessMetrics, playerBehavior, campaignPerformances, cancellationToken);
        
        // Generate predictive analytics
        var predictions = await GeneratePredictiveAnalytics(businessId, businessMetrics, playerBehavior, cancellationToken);
        
        // Calculate competitive benchmarks
        var benchmarks = await CalculateCompetitiveBenchmarks(businessMetrics, playerBehavior, cancellationToken);
        
        // Generate advanced player insights
        var advancedPlayerInsights = await GenerateAdvancedPlayerInsights(businessId, playerBehavior, start, end, cancellationToken);
        
        // Calculate revenue analytics
        var revenueAnalytics = await CalculateRevenueAnalytics(businessId, start, end, cancellationToken);
        
        // Generate optimization recommendations
        var recommendations = await GenerateOptimizationRecommendations(businessId, advancedPerformance, predictions, benchmarks, cancellationToken);

        return new PremiumAnalyticsResponse
        {
            BusinessId = businessId,
            BusinessName = business.Name,
            PeriodStart = start,
            PeriodEnd = end,
            SubscriptionTier = business.SubscriptionTier.ToString(),
            Performance = advancedPerformance,
            Predictions = predictions,
            Benchmarks = benchmarks,
            PlayerInsights = advancedPlayerInsights,
            Revenue = revenueAnalytics,
            Recommendations = recommendations
        };
    }

    public async Task<AdvancedCampaignComparisonResponse> GetAdvancedCampaignComparisonAsync(Guid businessId, List<Guid> campaignIds, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var business = await _context.Businesses
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

        if (business == null)
            throw new ArgumentException($"Business with ID {businessId} not found");

        // Verify subscription tier
        if (business.SubscriptionTier < SubscriptionTier.Premium)
            throw new UnauthorizedAccessException("Premium subscription required for advanced campaign comparison");

        // Get detailed performance for each campaign
        var campaignPerformances = new List<DetailedCampaignPerformanceResponse>();
        foreach (var campaignId in campaignIds)
        {
            try
            {
                var performance = await GetDetailedCampaignPerformanceAsync(campaignId, start, end, cancellationToken);
                campaignPerformances.Add(performance);
            }
            catch (ArgumentException)
            {
                // Skip campaigns that don't exist or don't belong to this business
                continue;
            }
        }

        // Perform statistical analysis
        var statisticalAnalysis = CalculateCampaignStatisticalAnalysis(campaignPerformances);
        
        // Generate performance rankings
        var performanceRankings = CalculateCampaignRankings(campaignPerformances);
        
        // Generate cross-campaign insights
        var crossCampaignInsights = CalculateCrossCampaignInsights(campaignPerformances);
        
        // Generate optimization opportunities
        var optimizationOpportunities = CalculateCampaignOptimizationOpportunities(campaignPerformances);

        return new AdvancedCampaignComparisonResponse
        {
            CampaignIds = campaignIds,
            PeriodStart = start,
            PeriodEnd = end,
            StatisticalAnalysis = statisticalAnalysis,
            PerformanceRankings = performanceRankings,
            CrossCampaignInsights = crossCampaignInsights,
            OptimizationOpportunities = optimizationOpportunities
        };
    }

    public async Task<List<OptimizationRecommendation>> GetOptimizationRecommendationsAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var business = await _context.Businesses
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

        if (business == null)
            throw new ArgumentException($"Business with ID {businessId} not found");

        // Verify subscription tier
        if (business.SubscriptionTier < SubscriptionTier.Premium)
            throw new UnauthorizedAccessException("Premium subscription required for optimization recommendations");

        // Get recent analytics data
        var businessMetrics = await GetBusinessMetricsAsync(businessId, cancellationToken: cancellationToken);
        var playerBehavior = await GetComprehensivePlayerBehaviorAsync(businessId, cancellationToken: cancellationToken);
        var campaignPerformances = await GetAllCampaignPerformancesAsync(businessId, cancellationToken: cancellationToken);

        // Calculate advanced performance metrics
        var advancedPerformance = await CalculateAdvancedPerformanceMetrics(businessId, businessMetrics, playerBehavior, campaignPerformances, cancellationToken);
        
        // Generate predictive analytics
        var predictions = await GeneratePredictiveAnalytics(businessId, businessMetrics, playerBehavior, cancellationToken);
        
        // Calculate competitive benchmarks
        var benchmarks = await CalculateCompetitiveBenchmarks(businessMetrics, playerBehavior, cancellationToken);

        return await GenerateOptimizationRecommendations(businessId, advancedPerformance, predictions, benchmarks, cancellationToken);
    }

    public async Task<PredictiveAnalytics> GetPredictiveAnalyticsAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var business = await _context.Businesses
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

        if (business == null)
            throw new ArgumentException($"Business with ID {businessId} not found");

        // Verify subscription tier
        if (business.SubscriptionTier < SubscriptionTier.Premium)
            throw new UnauthorizedAccessException("Premium subscription required for predictive analytics");

        // Get recent analytics data
        var businessMetrics = await GetBusinessMetricsAsync(businessId, cancellationToken: cancellationToken);
        var playerBehavior = await GetComprehensivePlayerBehaviorAsync(businessId, cancellationToken: cancellationToken);

        return await GeneratePredictiveAnalytics(businessId, businessMetrics, playerBehavior, cancellationToken);
    }

    // Premium Analytics Helper Methods

    private async Task<AdvancedPerformanceMetrics> CalculateAdvancedPerformanceMetrics(
        Guid businessId, 
        BusinessMetrics businessMetrics, 
        ComprehensivePlayerBehaviorResponse playerBehavior, 
        List<DetailedCampaignPerformanceResponse> campaignPerformances,
        CancellationToken cancellationToken)
    {
        // Mock advanced calculations - in a real implementation, these would be sophisticated algorithms
        var totalRevenue = businessMetrics.TotalTokenRevenue;
        var totalPlayers = businessMetrics.UniquePlayers;
        var totalCampaigns = campaignPerformances.Count;

        // Calculate ROI (simplified)
        var estimatedCosts = totalCampaigns * 100; // Mock campaign costs
        var roi = estimatedCosts > 0 ? (decimal)totalRevenue / estimatedCosts - 1 : 0;

        // Calculate engagement score (composite metric)
        var avgRedemptionRate = campaignPerformances.Any() ? campaignPerformances.Average(cp => cp.Performance.RedemptionRate) : 0;
        var avgSpinsPerPlayer = campaignPerformances.Any() ? campaignPerformances.Average(cp => cp.Performance.AverageSpinsPerPlayer) : 0;
        var engagementScore = Math.Min(100, (double)(avgRedemptionRate * 50 + (decimal)avgSpinsPerPlayer * 10));

        return new AdvancedPerformanceMetrics
        {
            ReturnOnInvestment = roi,
            CustomerAcquisitionCost = totalPlayers > 0 ? estimatedCosts / totalPlayers : 0,
            CustomerLifetimeValue = totalPlayers > 0 ? totalRevenue / totalPlayers * 3 : 0, // Mock CLV calculation
            RevenuePerPlayer = totalPlayers > 0 ? totalRevenue / totalPlayers : 0,
            ProfitMargin = totalRevenue > 0 ? (totalRevenue - estimatedCosts) / totalRevenue : 0,
            EngagementScore = engagementScore,
            StickinessIndex = 0.65, // Mock DAU/MAU ratio
            ViralityCoefficient = 0.15, // Mock virality
            AverageTimeToFirstWin = TimeSpan.FromMinutes(12),
            AverageTimeToRedemption = TimeSpan.FromHours(2.5),
            OptimalSpinPrice = 5, // Mock optimal pricing
            ConversionRatesBySegment = new Dictionary<string, decimal>
            {
                { "New Players", 0.25m },
                { "Regular Players", 0.45m },
                { "VIP Players", 0.75m }
            },
            ConversionRatesByHour = Enumerable.Range(0, 24).ToDictionary(h => h, h => 0.3m + (decimal)(Math.Sin(h * Math.PI / 12) * 0.2)),
            ConversionRatesByDayOfWeek = new Dictionary<string, decimal>
            {
                { "Monday", 0.35m }, { "Tuesday", 0.32m }, { "Wednesday", 0.38m },
                { "Thursday", 0.42m }, { "Friday", 0.55m }, { "Saturday", 0.65m }, { "Sunday", 0.48m }
            }
        };
    }

    private async Task<PredictiveAnalytics> GeneratePredictiveAnalytics(
        Guid businessId, 
        BusinessMetrics businessMetrics, 
        ComprehensivePlayerBehaviorResponse playerBehavior,
        CancellationToken cancellationToken)
    {
        // Mock predictive calculations - in reality, these would use ML models
        var currentGrowthRate = 0.15; // Mock 15% monthly growth
        var currentPlayers = businessMetrics.UniquePlayers;
        var currentRevenue = businessMetrics.TotalTokenRevenue;

        var predictions = new PredictiveAnalytics
        {
            PredictedNewPlayersNextMonth = (int)(currentPlayers * currentGrowthRate),
            PredictedChurnRate = playerBehavior.BehavioralInsights.ChurnRate * 1.1m, // Slight increase
            PredictedActivePlayersNextWeek = (int)(playerBehavior.Demographics.ActivePlayersThisWeek * 1.05),
            PredictedRevenueNextMonth = currentRevenue * (1 + (decimal)currentGrowthRate),
            PredictedTokenSalesNextWeek = currentRevenue * 0.25m, // 25% of monthly revenue per week
            CampaignPredictions = new Dictionary<Guid, CampaignPrediction>(),
            SeasonalTrends = new List<SeasonalTrend>
            {
                new() { Period = "Holiday Season", ImpactMultiplier = 1.8m, Description = "Increased engagement during holidays", NextOccurrence = new DateTime(DateTime.Now.Year, 12, 1) },
                new() { Period = "Summer", ImpactMultiplier = 0.85m, Description = "Reduced indoor activity", NextOccurrence = new DateTime(DateTime.Now.Year + (DateTime.Now.Month > 6 ? 1 : 0), 6, 1) },
                new() { Period = "Weekend", ImpactMultiplier = 1.3m, Description = "Higher weekend engagement", NextOccurrence = DateTime.Now.AddDays(6 - (int)DateTime.Now.DayOfWeek) }
            },
            RiskIndicators = new List<RiskIndicator>()
        };

        // Add risk indicators based on current metrics
        if (playerBehavior.BehavioralInsights.ChurnRate > 0.3m)
        {
            predictions.RiskIndicators.Add(new RiskIndicator
            {
                Type = "ChurnRisk",
                Severity = "High",
                Description = "Churn rate is above industry average",
                RecommendedActions = new List<string> { "Implement retention campaigns", "Improve prize quality", "Reduce spin costs" },
                Probability = 0.75m
            });
        }

        if (businessMetrics.OverallRedemptionRate < 0.4m)
        {
            predictions.RiskIndicators.Add(new RiskIndicator
            {
                Type = "EngagementDrop",
                Severity = "Medium",
                Description = "Low redemption rates indicate poor engagement",
                RecommendedActions = new List<string> { "Review prize offerings", "Simplify redemption process", "Add more valuable prizes" },
                Probability = 0.65m
            });
        }

        return predictions;
    }

    private async Task<CompetitiveBenchmarks> CalculateCompetitiveBenchmarks(
        BusinessMetrics businessMetrics, 
        ComprehensivePlayerBehaviorResponse playerBehavior,
        CancellationToken cancellationToken)
    {
        // Mock industry benchmarks - in reality, these would come from market research data
        return new CompetitiveBenchmarks
        {
            IndustryAverageRedemptionRate = 0.45m,
            IndustryAverageEngagementScore = 65.0,
            IndustryAverageRevenuePerPlayer = 25.0m,
            IndustryAverageSessionDuration = TimeSpan.FromMinutes(8),
            RedemptionRateRanking = businessMetrics.OverallRedemptionRate switch
            {
                >= 0.7m => "Top 10%",
                >= 0.5m => "Above Average",
                >= 0.3m => "Average",
                _ => "Below Average"
            },
            EngagementRanking = "Above Average", // Mock ranking
            RevenueRanking = "Average", // Mock ranking
            MarketPosition = "Challenger",
            CompetitiveAdvantages = new List<string>
            {
                "Strong local presence",
                "Innovative game mechanics",
                "High-quality prizes"
            },
            ImprovementOpportunities = new List<string>
            {
                "Expand to more locations",
                "Improve mobile experience",
                "Enhance social features"
            }
        };
    }

    private async Task<AdvancedPlayerInsights> GenerateAdvancedPlayerInsights(
        Guid businessId, 
        ComprehensivePlayerBehaviorResponse playerBehavior, 
        DateTime start, 
        DateTime end,
        CancellationToken cancellationToken)
    {
        // Mock advanced player insights - in reality, these would involve complex analytics
        return new AdvancedPlayerInsights
        {
            LifetimeValueBySegment = new Dictionary<string, decimal>
            {
                { "New Players", 15.0m },
                { "Regular Players", 45.0m },
                { "VIP Players", 120.0m }
            },
            PlayerCountBySegment = new Dictionary<string, int>
            {
                { "New Players", playerBehavior.Demographics.NewPlayersThisPeriod },
                { "Regular Players", (int)(playerBehavior.Demographics.TotalPlayers * 0.6) },
                { "VIP Players", (int)(playerBehavior.Demographics.TotalPlayers * 0.1) }
            },
            CohortAnalysis = GenerateMockCohortAnalysis(),
            PlayerJourney = new PlayerJourneyMetrics
            {
                AverageTimeToFirstSpin = TimeSpan.FromMinutes(5),
                AverageTimeToFirstWin = TimeSpan.FromMinutes(15),
                AverageTimeToFirstRedemption = TimeSpan.FromHours(3),
                FirstSpinConversionRate = 0.85m,
                FirstWinConversionRate = 0.35m,
                DropoffPointsByStage = new Dictionary<string, int>
                {
                    { "Registration", 100 },
                    { "First QR Scan", 85 },
                    { "First Spin", 72 },
                    { "First Win", 45 },
                    { "First Redemption", 32 }
                }
            },
            ChurnAnalysis = new ChurnAnalysis
            {
                CurrentChurnRate = playerBehavior.BehavioralInsights.ChurnRate,
                PredictedChurnRate = playerBehavior.BehavioralInsights.ChurnRate * 1.1m,
                ChurnRiskFactors = new List<ChurnRiskFactor>
                {
                    new() { Factor = "Low Win Rate", Impact = 0.4m, Description = "Players with low win rates are more likely to churn" },
                    new() { Factor = "Long Redemption Time", Impact = 0.3m, Description = "Delays in redemption increase churn risk" },
                    new() { Factor = "Limited Prize Variety", Impact = 0.2m, Description = "Lack of diverse prizes reduces engagement" }
                },
                PlayersAtRisk = new List<Guid>(), // Would be populated with actual at-risk player IDs
                ChurnRateBySegment = new Dictionary<string, decimal>
                {
                    { "New Players", 0.45m },
                    { "Regular Players", 0.25m },
                    { "VIP Players", 0.10m }
                }
            },
            Satisfaction = new PlayerSatisfactionMetrics
            {
                OverallSatisfactionScore = 78.5,
                GameplayExperienceScore = 82.0,
                PrizeQualityScore = 75.0,
                RedemptionExperienceScore = 79.0,
                TopCompliments = new List<string> { "Fun games", "Great prizes", "Easy to use" },
                TopComplaints = new List<string> { "Long wait times", "Limited prize variety", "App crashes" }
            }
        };
    }

    private List<CohortAnalysis> GenerateMockCohortAnalysis()
    {
        var cohorts = new List<CohortAnalysis>();
        var now = DateTime.UtcNow;
        
        for (int i = 0; i < 6; i++)
        {
            var cohortMonth = now.AddMonths(-i);
            var initialCount = 100 - (i * 10); // Mock decreasing cohort sizes
            
            var retentionByMonth = new Dictionary<int, decimal>();
            var revenueByMonth = new Dictionary<int, decimal>();
            
            for (int month = 0; month <= i; month++)
            {
                var retentionRate = Math.Max(0.1m, 1.0m - (month * 0.15m)); // Mock retention decay
                var monthlyRevenue = initialCount * retentionRate * 5; // Mock revenue calculation
                
                retentionByMonth[month] = retentionRate;
                revenueByMonth[month] = monthlyRevenue;
            }
            
            cohorts.Add(new CohortAnalysis
            {
                CohortMonth = cohortMonth,
                InitialPlayerCount = initialCount,
                RetentionByMonth = retentionByMonth,
                RevenueByMonth = revenueByMonth
            });
        }
        
        return cohorts;
    }

    private async Task<RevenueAnalytics> CalculateRevenueAnalytics(Guid businessId, DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        // Mock revenue analytics - in reality, this would integrate with payment systems
        var businessMetrics = await GetBusinessMetricsAsync(businessId, start, end, cancellationToken);
        var totalRevenue = businessMetrics.TotalTokenRevenue;
        
        return new RevenueAnalytics
        {
            TotalRevenue = totalRevenue,
            TokenSalesRevenue = totalRevenue * 0.8m,
            SubscriptionRevenue = totalRevenue * 0.15m,
            CommissionRevenue = totalRevenue * 0.05m,
            DailyRevenue = GenerateMockDailyRevenue(start, end, totalRevenue),
            WeeklyRevenue = GenerateMockWeeklyRevenue(start, end, totalRevenue),
            MonthlyRevenue = GenerateMockMonthlyRevenue(start, end, totalRevenue),
            OptimalTokenPrice = 5.0m,
            RevenueByPlayerSegment = new Dictionary<string, decimal>
            {
                { "New Players", totalRevenue * 0.2m },
                { "Regular Players", totalRevenue * 0.5m },
                { "VIP Players", totalRevenue * 0.3m }
            },
            RevenueByPrizeCategory = new Dictionary<string, decimal>
            {
                { "Food & Beverage", totalRevenue * 0.4m },
                { "Retail", totalRevenue * 0.3m },
                { "Services", totalRevenue * 0.2m },
                { "Entertainment", totalRevenue * 0.1m }
            },
            RevenueGrowthRate = 0.15m,
            RevenueVolatility = 0.25m,
            SeasonalityIndex = 1.2m
        };
    }

    private List<RevenueByPeriod> GenerateMockDailyRevenue(DateTime start, DateTime end, decimal totalRevenue)
    {
        var dailyRevenue = new List<RevenueByPeriod>();
        var days = (end - start).Days;
        var avgDailyRevenue = days > 0 ? totalRevenue / days : 0;
        
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            var variance = (decimal)(new Random().NextDouble() * 0.4 - 0.2); // ±20% variance
            var dayRevenue = avgDailyRevenue * (1 + variance);
            
            dailyRevenue.Add(new RevenueByPeriod
            {
                Period = date,
                Revenue = Math.Max(0, dayRevenue),
                GrowthRate = variance,
                TransactionCount = (int)(dayRevenue / 5), // Mock transaction count
                AverageTransactionValue = dayRevenue > 0 ? 5.0m : 0
            });
        }
        
        return dailyRevenue;
    }

    private List<RevenueByPeriod> GenerateMockWeeklyRevenue(DateTime start, DateTime end, decimal totalRevenue)
    {
        // Mock weekly revenue calculation
        var weeks = Math.Max(1, (int)Math.Ceiling((end - start).TotalDays / 7));
        var avgWeeklyRevenue = totalRevenue / weeks;
        
        return new List<RevenueByPeriod>
        {
            new() { Period = start, Revenue = avgWeeklyRevenue, GrowthRate = 0.1m, TransactionCount = 50, AverageTransactionValue = 5.0m }
        };
    }

    private List<RevenueByPeriod> GenerateMockMonthlyRevenue(DateTime start, DateTime end, decimal totalRevenue)
    {
        // Mock monthly revenue calculation
        return new List<RevenueByPeriod>
        {
            new() { Period = start, Revenue = totalRevenue, GrowthRate = 0.15m, TransactionCount = 200, AverageTransactionValue = 5.0m }
        };
    }

    private async Task<List<OptimizationRecommendation>> GenerateOptimizationRecommendations(
        Guid businessId, 
        AdvancedPerformanceMetrics performance, 
        PredictiveAnalytics predictions, 
        CompetitiveBenchmarks benchmarks,
        CancellationToken cancellationToken)
    {
        var recommendations = new List<OptimizationRecommendation>();

        // Pricing optimization
        if (performance.RevenuePerPlayer < benchmarks.IndustryAverageRevenuePerPlayer)
        {
            recommendations.Add(new OptimizationRecommendation
            {
                Category = "Pricing",
                Title = "Optimize Token Pricing",
                Description = "Your revenue per player is below industry average. Consider adjusting token prices or spin costs.",
                Priority = "High",
                PotentialImpact = 0.25m,
                ImpactType = "Revenue",
                ActionItems = new List<string>
                {
                    "A/B test different token price points",
                    "Implement dynamic pricing based on demand",
                    "Offer bulk token purchase discounts"
                },
                EstimatedImplementationDays = 14,
                ConfidenceScore = 0.8m
            });
        }

        // Engagement optimization
        if (performance.EngagementScore < 70)
        {
            recommendations.Add(new OptimizationRecommendation
            {
                Category = "Player Engagement",
                Title = "Improve Player Engagement",
                Description = "Your engagement score is below optimal levels. Focus on retention strategies.",
                Priority = "High",
                PotentialImpact = 0.35m,
                ImpactType = "Engagement",
                ActionItems = new List<string>
                {
                    "Introduce daily login bonuses",
                    "Add social features and leaderboards",
                    "Implement push notifications for inactive players"
                },
                EstimatedImplementationDays = 21,
                ConfidenceScore = 0.75m
            });
        }

        // Campaign optimization
        recommendations.Add(new OptimizationRecommendation
        {
            Category = "Campaigns",
            Title = "Optimize Campaign Timing",
            Description = "Launch campaigns during peak engagement hours to maximize participation.",
            Priority = "Medium",
            PotentialImpact = 0.15m,
            ImpactType = "Engagement",
            ActionItems = new List<string>
            {
                "Schedule campaigns for peak hours (6-8 PM)",
                "Create weekend-specific campaigns",
                "Implement seasonal campaign themes"
            },
            EstimatedImplementationDays = 7,
            ConfidenceScore = 0.9m
        });

        // Churn reduction
        if (predictions.RiskIndicators.Any(r => r.Type == "ChurnRisk"))
        {
            recommendations.Add(new OptimizationRecommendation
            {
                Category = "Retention",
                Title = "Implement Churn Prevention",
                Description = "High churn risk detected. Implement retention strategies for at-risk players.",
                Priority = "High",
                PotentialImpact = 0.30m,
                ImpactType = "Retention",
                ActionItems = new List<string>
                {
                    "Create win-back campaigns for inactive players",
                    "Implement personalized prize recommendations",
                    "Add loyalty program with tier benefits"
                },
                EstimatedImplementationDays = 28,
                ConfidenceScore = 0.7m
            });
        }

        return recommendations.OrderByDescending(r => r.PotentialImpact * r.ConfidenceScore).ToList();
    }

    // Advanced Campaign Comparison Helper Methods

    private CampaignStatisticalAnalysis CalculateCampaignStatisticalAnalysis(List<DetailedCampaignPerformanceResponse> campaigns)
    {
        if (!campaigns.Any()) return new CampaignStatisticalAnalysis();

        var redemptionRates = campaigns.Select(c => c.Performance.RedemptionRate).ToList();
        var engagementScores = campaigns.Select(c => (decimal)c.Performance.AverageSpinsPerPlayer).ToList();

        return new CampaignStatisticalAnalysis
        {
            AverageRedemptionRate = redemptionRates.Average(),
            StandardDeviationRedemptionRate = CalculateStandardDeviation(redemptionRates),
            AverageEngagementScore = engagementScores.Average(),
            StandardDeviationEngagementScore = CalculateStandardDeviation(engagementScores),
            BestPerformingMetric = "Redemption Rate", // Mock analysis
            WorstPerformingMetric = "Player Retention", // Mock analysis
            CorrelationSpinsToRevenue = 0.85m, // Mock correlation
            CorrelationPrizeValueToRedemption = 0.65m // Mock correlation
        };
    }

    private decimal CalculateStandardDeviation(List<decimal> values)
    {
        if (!values.Any()) return 0;
        
        var average = values.Average();
        var sumOfSquares = values.Sum(v => (v - average) * (v - average));
        return (decimal)Math.Sqrt((double)(sumOfSquares / values.Count));
    }

    private List<CampaignRanking> CalculateCampaignRankings(List<DetailedCampaignPerformanceResponse> campaigns)
    {
        var rankings = new List<CampaignRanking>();
        
        foreach (var campaign in campaigns)
        {
            var overallScore = (double)(
                campaign.Performance.RedemptionRate * 30 +
                (decimal)campaign.Performance.AverageSpinsPerPlayer * 20 +
                campaign.Performance.WinRate * 25 +
                (campaign.Performance.TokenRevenue / Math.Max(1, campaign.Performance.TotalSpins)) * 25
            );

            rankings.Add(new CampaignRanking
            {
                CampaignId = campaign.CampaignId,
                CampaignName = campaign.CampaignName,
                OverallScore = overallScore,
                MetricRankings = new Dictionary<string, int>
                {
                    { "Redemption Rate", 1 }, // Mock rankings
                    { "Engagement", 2 },
                    { "Revenue", 1 }
                },
                Strengths = new List<string> { "High engagement", "Good prize variety" },
                Weaknesses = new List<string> { "Low conversion rate", "Limited reach" }
            });
        }

        // Assign overall ranks
        var sortedRankings = rankings.OrderByDescending(r => r.OverallScore).ToList();
        for (int i = 0; i < sortedRankings.Count; i++)
        {
            sortedRankings[i].OverallRank = i + 1;
        }

        return sortedRankings;
    }

    private CrossCampaignInsights CalculateCrossCampaignInsights(List<DetailedCampaignPerformanceResponse> campaigns)
    {
        // Mock cross-campaign analysis
        return new CrossCampaignInsights
        {
            MostSuccessfulCampaignType = "Wheel of Luck",
            OptimalCampaignDuration = "2-3 weeks",
            OptimalPrizeValueRange = 15.0m,
            BestPerformingTimeSlot = "6-8 PM",
            SuccessPatterns = new List<string>
            {
                "Campaigns with food prizes perform 40% better",
                "Weekend launches show 25% higher engagement",
                "Limited-time offers increase urgency by 60%"
            },
            FailurePatterns = new List<string>
            {
                "Campaigns longer than 4 weeks show declining engagement",
                "Low-value prizes result in poor redemption rates",
                "Complex rules reduce participation by 30%"
            }
        };
    }

    private List<CampaignOptimizationOpportunity> CalculateCampaignOptimizationOpportunities(List<DetailedCampaignPerformanceResponse> campaigns)
    {
        var opportunities = new List<CampaignOptimizationOpportunity>();

        foreach (var campaign in campaigns)
        {
            if (campaign.Performance.RedemptionRate < 0.4m)
            {
                opportunities.Add(new CampaignOptimizationOpportunity
                {
                    CampaignId = campaign.CampaignId,
                    CampaignName = campaign.CampaignName,
                    OpportunityType = "Redemption Rate",
                    Description = "Low redemption rate indicates poor prize appeal or complex redemption process",
                    PotentialImprovement = 0.25m,
                    RecommendedAction = "Simplify redemption process and improve prize quality",
                    Priority = 4
                });
            }

            if (campaign.Performance.AverageSpinsPerPlayer < 2.0)
            {
                opportunities.Add(new CampaignOptimizationOpportunity
                {
                    CampaignId = campaign.CampaignId,
                    CampaignName = campaign.CampaignName,
                    OpportunityType = "Player Engagement",
                    Description = "Low average spins per player suggests poor engagement",
                    PotentialImprovement = 0.35m,
                    RecommendedAction = "Reduce spin costs or improve win rates",
                    Priority = 5
                });
            }
        }

        return opportunities.OrderByDescending(o => o.Priority).ToList();
    }

    // Multi-Location Analytics Methods

    public async Task<MultiLocationBusinessMetrics> GetMultiLocationBusinessMetricsAsync(Guid businessId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var business = await _context.Businesses
            .Include(b => b.BusinessLocations)
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

        if (business == null)
            throw new ArgumentException($"Business with ID {businessId} not found");

        var locations = business.BusinessLocations.Where(bl => bl.IsActive).ToList();
        var locationMetrics = new List<LocationMetrics>();

        // Get metrics for each location
        foreach (var location in locations)
        {
            var locationQRScans = await _context.QRSessions
                .Where(qr => qr.BusinessId == businessId && qr.CreatedAt >= start && qr.CreatedAt <= end)
                .ToListAsync(cancellationToken);

            // Filter QR scans by location proximity (within 100m)
            var locationSpecificScans = locationQRScans
                .Where(qr => qr.PlayerLocation != null &&
                           location.Location.IsWithinRadius(qr.PlayerLocation, 100))
                .ToList();

            var campaignIds = await _context.Campaigns
                .Where(c => c.BusinessId == businessId && 
                           (c.Targeting == CampaignTargeting.AllLocations || 
                            c.CampaignLocations.Any(cl => cl.LocationId == location.Id)))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            var wheelSpins = await _context.WheelSpins
                .Where(ws => campaignIds.Contains(ws.CampaignId) && ws.CreatedAt >= start && ws.CreatedAt <= end)
                .ToListAsync(cancellationToken);

            var prizeIds = await _context.Prizes
                .Where(p => campaignIds.Contains(p.CampaignId))
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            var redemptions = await _context.PlayerPrizes
                .Where(pp => prizeIds.Contains(pp.PrizeId) && pp.IsRedeemed && pp.RedeemedAt >= start && pp.RedeemedAt <= end)
                .CountAsync(cancellationToken);

            var uniquePlayers = locationSpecificScans.Select(qr => qr.PlayerId)
                .Union(wheelSpins.Select(ws => ws.PlayerId))
                .Distinct()
                .Count();

            var prizesAwarded = wheelSpins.Count(ws => ws.PrizeId.HasValue);

            locationMetrics.Add(new LocationMetrics
            {
                LocationId = location.Id,
                LocationName = location.Name,
                Address = location.Address,
                QRScans = locationSpecificScans.Count,
                Spins = wheelSpins.Count,
                UniquePlayers = uniquePlayers,
                PrizesAwarded = prizesAwarded,
                PrizesRedeemed = redemptions,
                RedemptionRate = prizesAwarded > 0 ? (decimal)redemptions / prizesAwarded : 0,
                TokenRevenue = wheelSpins.Sum(ws => ws.TokensSpent),
                AverageSpinsPerPlayer = uniquePlayers > 0 ? (double)wheelSpins.Count / uniquePlayers : 0
            });
        }

        // Calculate consolidated metrics
        var totalQRScans = locationMetrics.Sum(lm => lm.QRScans);
        var totalSpins = locationMetrics.Sum(lm => lm.Spins);
        var totalUniquePlayers = locationMetrics.SelectMany(lm => new[] { lm.UniquePlayers }).Sum(); // Simplified - would need deduplication
        var totalPrizesAwarded = locationMetrics.Sum(lm => lm.PrizesAwarded);
        var totalPrizesRedeemed = locationMetrics.Sum(lm => lm.PrizesRedeemed);
        var totalTokenRevenue = locationMetrics.Sum(lm => lm.TokenRevenue);

        return new MultiLocationBusinessMetrics
        {
            BusinessId = businessId,
            BusinessName = business.Name,
            PeriodStart = start,
            PeriodEnd = end,
            TotalLocations = locations.Count,
            ActiveLocations = locations.Count(l => locationMetrics.Any(lm => lm.LocationId == l.Id && lm.QRScans > 0)),
            ConsolidatedMetrics = new BusinessMetrics
            {
                BusinessId = businessId,
                BusinessName = business.Name,
                TotalQRScans = totalQRScans,
                TotalSpins = totalSpins,
                UniquePlayers = totalUniquePlayers,
                TotalPrizesAwarded = totalPrizesAwarded,
                TotalPrizesRedeemed = totalPrizesRedeemed,
                OverallRedemptionRate = totalPrizesAwarded > 0 ? (decimal)totalPrizesRedeemed / totalPrizesAwarded : 0,
                TotalTokenRevenue = totalTokenRevenue,
                ActiveCampaigns = await _context.Campaigns.Where(c => c.BusinessId == businessId && c.IsActive).CountAsync(cancellationToken),
                PeriodStart = start,
                PeriodEnd = end
            },
            LocationMetrics = locationMetrics,
            TopPerformingLocation = locationMetrics.OrderByDescending(lm => lm.TokenRevenue).FirstOrDefault(),
            LowestPerformingLocation = locationMetrics.OrderBy(lm => lm.TokenRevenue).FirstOrDefault(),
            LocationPerformanceComparison = CalculateLocationPerformanceComparison(locationMetrics)
        };
    }

    public async Task<List<CampaignLocationPerformance>> GetCampaignLocationPerformanceAsync(Guid campaignId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var campaign = await _context.Campaigns
            .Include(c => c.Business)
            .ThenInclude(b => b.BusinessLocations)
            .Include(c => c.CampaignLocations)
            .ThenInclude(cl => cl.Location)
            .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);

        if (campaign == null)
            throw new ArgumentException($"Campaign with ID {campaignId} not found");

        var targetedLocations = campaign.Targeting == CampaignTargeting.AllLocations
            ? campaign.Business.BusinessLocations.Where(bl => bl.IsActive).ToList()
            : campaign.CampaignLocations.Select(cl => cl.Location).Where(l => l.IsActive).ToList();

        var locationPerformances = new List<CampaignLocationPerformance>();

        foreach (var location in targetedLocations)
        {
            // Get QR scans near this location
            var locationQRScans = await _context.QRSessions
                .Where(qr => qr.BusinessId == campaign.BusinessId && qr.CreatedAt >= start && qr.CreatedAt <= end &&
                           qr.PlayerLocation != null)
                .ToListAsync(cancellationToken);

            var locationSpecificScans = locationQRScans
                .Where(qr => location.Location.IsWithinRadius(qr.PlayerLocation!, 100))
                .ToList();

            // Get wheel spins for this campaign
            var wheelSpins = await _context.WheelSpins
                .Where(ws => ws.CampaignId == campaignId && ws.CreatedAt >= start && ws.CreatedAt <= end)
                .ToListAsync(cancellationToken);

            // Get prize redemptions for this campaign at this location
            var prizeIds = await _context.Prizes
                .Where(p => p.CampaignId == campaignId)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            var redemptions = await _context.PlayerPrizes
                .Where(pp => prizeIds.Contains(pp.PrizeId) && pp.IsRedeemed && pp.RedeemedAt >= start && pp.RedeemedAt <= end)
                .CountAsync(cancellationToken);

            var uniquePlayers = locationSpecificScans.Select(qr => qr.PlayerId)
                .Union(wheelSpins.Select(ws => ws.PlayerId))
                .Distinct()
                .Count();

            var prizesAwarded = wheelSpins.Count(ws => ws.PrizeId.HasValue);

            locationPerformances.Add(new CampaignLocationPerformance
            {
                LocationId = location.Id,
                LocationName = location.Name,
                Address = location.Address,
                CampaignId = campaignId,
                CampaignName = campaign.Name,
                QRScans = locationSpecificScans.Count,
                Spins = wheelSpins.Count,
                UniquePlayers = uniquePlayers,
                PrizesAwarded = prizesAwarded,
                PrizesRedeemed = redemptions,
                RedemptionRate = prizesAwarded > 0 ? (decimal)redemptions / prizesAwarded : 0,
                TokenRevenue = wheelSpins.Sum(ws => ws.TokensSpent),
                AverageSpinsPerPlayer = uniquePlayers > 0 ? (double)wheelSpins.Count / uniquePlayers : 0,
                ConversionRate = locationSpecificScans.Count > 0 ? (decimal)wheelSpins.Count / locationSpecificScans.Count : 0
            });
        }

        return locationPerformances.OrderByDescending(lp => lp.TokenRevenue).ToList();
    }

    private LocationPerformanceComparison CalculateLocationPerformanceComparison(List<LocationMetrics> locationMetrics)
    {
        if (!locationMetrics.Any()) return new LocationPerformanceComparison();

        var avgQRScans = locationMetrics.Average(lm => lm.QRScans);
        var avgSpins = locationMetrics.Average(lm => lm.Spins);
        var avgRevenue = locationMetrics.Average(lm => lm.TokenRevenue);
        var avgRedemptionRate = locationMetrics.Average(lm => lm.RedemptionRate);

        return new LocationPerformanceComparison
        {
            AverageQRScansPerLocation = avgQRScans,
            AverageSpinsPerLocation = avgSpins,
            AverageRevenuePerLocation = avgRevenue,
            AverageRedemptionRate = avgRedemptionRate,
            PerformanceVariance = new LocationPerformanceVariance
            {
                QRScansVariance = CalculateVariance(locationMetrics.Select(lm => (double)lm.QRScans)),
                SpinsVariance = CalculateVariance(locationMetrics.Select(lm => (double)lm.Spins)),
                RevenueVariance = CalculateVariance(locationMetrics.Select(lm => (double)lm.TokenRevenue)),
                RedemptionRateVariance = CalculateVariance(locationMetrics.Select(lm => (double)lm.RedemptionRate))
            },
            LocationRankings = locationMetrics
                .OrderByDescending(lm => lm.TokenRevenue)
                .Select((lm, index) => new LocationRanking
                {
                    LocationId = lm.LocationId,
                    LocationName = lm.LocationName,
                    Rank = index + 1,
                    Score = CalculateLocationScore(lm, avgQRScans, avgSpins, avgRevenue, avgRedemptionRate)
                })
                .ToList()
        };
    }

    private double CalculateVariance(IEnumerable<double> values)
    {
        var valuesList = values.ToList();
        if (!valuesList.Any()) return 0;

        var mean = valuesList.Average();
        var sumOfSquaredDifferences = valuesList.Sum(val => Math.Pow(val - mean, 2));
        return sumOfSquaredDifferences / valuesList.Count;
    }

    private double CalculateLocationScore(LocationMetrics location, double avgQRScans, double avgSpins, double avgRevenue, decimal avgRedemptionRate)
    {
        var qrScansScore = avgQRScans > 0 ? location.QRScans / avgQRScans : 0;
        var spinsScore = avgSpins > 0 ? location.Spins / avgSpins : 0;
        var revenueScore = avgRevenue > 0 ? (double)location.TokenRevenue / avgRevenue : 0;
        var redemptionScore = avgRedemptionRate > 0 ? (double)location.RedemptionRate / (double)avgRedemptionRate : 0;

        return (qrScansScore * 0.25 + spinsScore * 0.25 + revenueScore * 0.35 + redemptionScore * 0.15) * 100;
    }
}