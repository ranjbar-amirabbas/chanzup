using Microsoft.EntityFrameworkCore;
using Chanzup.Application.DTOs;
using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;
using Chanzup.Domain.ValueObjects;

namespace Chanzup.Application.Services;

public class QRSessionService : IQRSessionService
{
    private readonly IApplicationDbContext _context;
    private readonly IQRCodeService _qrCodeService;
    private readonly IAntiFraudService _antiFraudService;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly IAnalyticsService _analyticsService;
    private const int DEFAULT_COOLDOWN_MINUTES = 30;
    private const int DEFAULT_DAILY_TOKEN_LIMIT = 100;
    private const int DEFAULT_TOKENS_PER_SCAN = 10;
    private const double LOCATION_TOLERANCE_METERS = 100.0;

    public QRSessionService(
        IApplicationDbContext context, 
        IQRCodeService qrCodeService,
        IAntiFraudService antiFraudService,
        IRateLimitingService rateLimitingService,
        IAnalyticsService analyticsService)
    {
        _context = context;
        _qrCodeService = qrCodeService;
        _antiFraudService = antiFraudService;
        _rateLimitingService = rateLimitingService;
        _analyticsService = analyticsService;
    }

    public async Task<QRScanResponse> ProcessQRScanAsync(Guid playerId, QRScanRequest request)
    {
        // Rate limiting check
        var rateLimitKey = $"qr_scan:{playerId}";
        if (!await _rateLimitingService.IsWithinRateLimitAsync(rateLimitKey, 10, TimeSpan.FromMinutes(1)))
        {
            return new QRScanResponse
            {
                Message = "Rate limit exceeded. Please wait before scanning again."
            };
        }

        // Anti-fraud validation
        var fraudResult = await _antiFraudService.ValidateQRScanAsync(playerId, request);
        if (!fraudResult.IsValid)
        {
            return new QRScanResponse
            {
                Message = fraudResult.Reason
            };
        }

        // Validate QR code format and extract campaign/business ID
        if (!_qrCodeService.ValidateQRCode(request.QRCode, out var campaignId))
        {
            return new QRScanResponse
            {
                Message = "Invalid QR code format"
            };
        }

        // Get campaign and business information
        var campaign = await _context.Campaigns
            .Include(c => c.Business)
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.IsActive);

        if (campaign == null)
        {
            return new QRScanResponse
            {
                Message = "Campaign not found or inactive"
            };
        }

        var businessId = campaign.BusinessId;

        // Validate player exists and is active
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.Id == playerId && p.IsActive);

        if (player == null)
        {
            return new QRScanResponse
            {
                Message = "Player not found or inactive"
            };
        }

        // Check if player can scan (cooldown, daily limits, etc.)
        if (!await CanPlayerScanAsync(playerId, businessId, request.Timestamp))
        {
            return new QRScanResponse
            {
                Message = "Cannot scan at this time. Please wait before scanning again."
            };
        }

        // Validate location
        if (!await ValidateLocationAsync(businessId, request.Latitude, request.Longitude))
        {
            return new QRScanResponse
            {
                Message = "You must be at the business location to scan this QR code"
            };
        }

        // Check daily token earning limit
        var tokensEarnedToday = await GetTokensEarnedTodayAsync(playerId, businessId);
        var dailyLimit = await GetDailyTokenLimitAsync(playerId, businessId);
        
        if (tokensEarnedToday >= dailyLimit)
        {
            return new QRScanResponse
            {
                Message = "Daily token earning limit reached for this business"
            };
        }

        // Calculate tokens to award (respecting daily limit)
        var tokensToAward = Math.Min(DEFAULT_TOKENS_PER_SCAN, dailyLimit - tokensEarnedToday);

        // Create QR session
        var sessionHash = QRSession.GenerateSessionHash(playerId, businessId, request.Timestamp);
        var qrSession = new QRSession
        {
            PlayerId = playerId,
            BusinessId = businessId,
            PlayerLocation = new Location(request.Latitude, request.Longitude),
            TokensEarned = tokensToAward,
            SessionHash = sessionHash,
            CreatedAt = request.Timestamp
        };

        _context.QRSessions.Add(qrSession);

        // Award tokens to player
        player.AddTokens(tokensToAward, $"QR scan at {campaign.Business.Name}");

        // Create token transaction record
        var tokenTransaction = new TokenTransaction
        {
            PlayerId = playerId,
            Type = TransactionType.Earned,
            Amount = tokensToAward,
            Description = $"QR scan at {campaign.Business.Name}",
            CreatedAt = request.Timestamp
        };

        _context.TokenTransactions.Add(tokenTransaction);

        await _context.SaveChangesAsync();

        // Track analytics event
        await _analyticsService.TrackQRScanAsync(businessId, playerId, campaignId, tokensToAward);

        // Record the request for rate limiting
        await _rateLimitingService.RecordRequestAsync(rateLimitKey);

        // Get remaining spins for today
        var remainingSpins = await GetRemainingSpinsTodayAsync(playerId, campaignId);

        return new QRScanResponse
        {
            SessionId = qrSession.Id,
            TokensEarned = tokensToAward,
            NewBalance = player.TokenBalance,
            CanSpin = player.CanAffordSpin(campaign.TokenCostPerSpin) && remainingSpins > 0,
            Campaign = new CampaignInfo
            {
                Id = campaign.Id,
                Name = campaign.Name,
                TokenCostPerSpin = campaign.TokenCostPerSpin,
                RemainingSpinsToday = remainingSpins
            },
            Message = $"Successfully earned {tokensToAward} tokens!"
        };
    }

    public async Task<bool> CanPlayerScanAsync(Guid playerId, Guid businessId, DateTime? scanTime = null)
    {
        // Check cooldown period
        if (await IsWithinCooldownPeriodAsync(playerId, businessId, scanTime))
        {
            return false;
        }

        // Check daily token limit
        var tokensEarnedToday = await GetTokensEarnedTodayAsync(playerId, businessId);
        var dailyLimit = await GetDailyTokenLimitAsync(playerId, businessId);

        return tokensEarnedToday < dailyLimit;
    }

    public async Task<int> GetRemainingSpinsTodayAsync(Guid playerId, Guid campaignId)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var campaign = await _context.Campaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign == null)
            return 0;

        var spinsToday = await _context.WheelSpins
            .CountAsync(ws => ws.PlayerId == playerId && 
                             ws.CampaignId == campaignId &&
                             ws.CreatedAt >= today && 
                             ws.CreatedAt < tomorrow);

        return Math.Max(0, campaign.MaxSpinsPerDay - spinsToday);
    }

    public async Task<bool> ValidateLocationAsync(Guid businessId, decimal playerLatitude, decimal playerLongitude)
    {
        var business = await _context.Businesses
            .FirstOrDefaultAsync(b => b.Id == businessId);

        if (business?.Location == null)
            return false;

        var distance = CalculateDistance(
            (double)playerLatitude, (double)playerLongitude,
            (double)business.Location.Latitude, (double)business.Location.Longitude);

        return distance <= LOCATION_TOLERANCE_METERS;
    }

    public async Task<bool> IsWithinCooldownPeriodAsync(Guid playerId, Guid businessId, DateTime? scanTime = null)
    {
        var referenceTime = scanTime ?? DateTime.UtcNow;
        var cooldownTime = referenceTime.AddMinutes(-DEFAULT_COOLDOWN_MINUTES);

        var recentSession = await _context.QRSessions
            .Where(qs => qs.PlayerId == playerId && 
                        qs.BusinessId == businessId &&
                        qs.CreatedAt > cooldownTime)
            .OrderByDescending(qs => qs.CreatedAt)
            .FirstOrDefaultAsync();

        return recentSession != null;
    }

    public async Task<int> GetDailyTokenLimitAsync(Guid playerId, Guid businessId)
    {
        // For now, return default limit. In future, this could be configurable per business
        await Task.CompletedTask;
        return DEFAULT_DAILY_TOKEN_LIMIT;
    }

    public async Task<int> GetTokensEarnedTodayAsync(Guid playerId, Guid businessId)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var tokensEarned = await _context.QRSessions
            .Where(qs => qs.PlayerId == playerId && 
                        qs.BusinessId == businessId &&
                        qs.CreatedAt >= today && 
                        qs.CreatedAt < tomorrow)
            .SumAsync(qs => qs.TokensEarned);

        return tokensEarned;
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth's radius in meters
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}