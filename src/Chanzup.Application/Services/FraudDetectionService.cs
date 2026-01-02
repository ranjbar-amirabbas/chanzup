using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;

namespace Chanzup.Application.Services;

public class FraudDetectionService : IFraudDetectionService
{
    private readonly IApplicationDbContext _context;
    private readonly ISystemParameterService _parameterService;
    private readonly ILogger<FraudDetectionService> _logger;

    public FraudDetectionService(
        IApplicationDbContext context,
        ISystemParameterService parameterService,
        ILogger<FraudDetectionService> logger)
    {
        _context = context;
        _parameterService = parameterService;
        _logger = logger;
    }

    // Suspicious Activity Detection
    public async Task<bool> AnalyzeQRScanAsync(Guid playerId, Guid businessId, string ipAddress, string? userAgent = null)
    {
        var riskScore = 0m;
        var suspiciousFactors = new List<string>();

        // Check scan frequency
        var recentScans = await _context.QRSessions
            .Where(qs => qs.PlayerId == playerId && qs.CreatedAt >= DateTime.UtcNow.AddHours(-1))
            .CountAsync();

        var maxScansPerHour = await _parameterService.GetIntParameterAsync("FRAUD_MAX_SCANS_PER_HOUR", 20);
        if (recentScans > maxScansPerHour)
        {
            riskScore += 30;
            suspiciousFactors.Add($"Excessive scanning: {recentScans} scans in last hour");
        }

        // Check location consistency
        var locationRisk = await CalculateLocationRiskScoreAsync(ipAddress);
        riskScore += locationRisk;

        // Check device consistency
        var deviceRisk = await CalculateDeviceRiskScoreAsync(userAgent, ipAddress);
        riskScore += deviceRisk;

        // Check for rapid location changes
        var lastSession = await _context.QRSessions
            .Where(qs => qs.PlayerId == playerId)
            .OrderByDescending(qs => qs.CreatedAt)
            .FirstOrDefaultAsync();

        if (lastSession != null && lastSession.CreatedAt >= DateTime.UtcNow.AddMinutes(-30))
        {
            // Check if locations are too far apart for the time difference
            if (await DetectLocationAnomaliesAsync(playerId, ipAddress))
            {
                riskScore += 25;
                suspiciousFactors.Add("Impossible travel detected");
            }
        }

        var riskThreshold = await _parameterService.GetDecimalParameterAsync("FRAUD_QR_RISK_THRESHOLD", 50m);
        if (riskScore >= riskThreshold)
        {
            await ReportSuspiciousActivityAsync(
                "QR_SCAN_FRAUD",
                $"Suspicious QR scan detected: {string.Join(", ", suspiciousFactors)}",
                riskScore >= 75 ? SuspiciousActivitySeverity.High : SuspiciousActivitySeverity.Medium,
                playerId,
                "Player",
                null,
                new { BusinessId = businessId, RiskScore = riskScore, Factors = suspiciousFactors },
                ipAddress,
                userAgent);

            return true;
        }

        return false;
    }

    public async Task<bool> AnalyzeWheelSpinAsync(Guid playerId, Guid campaignId, string ipAddress, string? userAgent = null)
    {
        var riskScore = 0m;
        var suspiciousFactors = new List<string>();

        // Check spin frequency
        var recentSpins = await _context.WheelSpins
            .Where(ws => ws.PlayerId == playerId && ws.CreatedAt >= DateTime.UtcNow.AddHours(-1))
            .CountAsync();

        var maxSpinsPerHour = await _parameterService.GetIntParameterAsync("FRAUD_MAX_SPINS_PER_HOUR", 50);
        if (recentSpins > maxSpinsPerHour)
        {
            riskScore += 40;
            suspiciousFactors.Add($"Excessive spinning: {recentSpins} spins in last hour");
        }

        // Check win rate anomalies
        var totalSpins = await _context.WheelSpins
            .Where(ws => ws.PlayerId == playerId)
            .CountAsync();

        var winningSpins = await _context.WheelSpins
            .Where(ws => ws.PlayerId == playerId && ws.PrizeId != null)
            .CountAsync();

        if (totalSpins > 10)
        {
            var winRate = (decimal)winningSpins / totalSpins;
            var expectedWinRate = await _parameterService.GetDecimalParameterAsync("FRAUD_EXPECTED_WIN_RATE", 0.3m);
            var winRateThreshold = await _parameterService.GetDecimalParameterAsync("FRAUD_WIN_RATE_THRESHOLD", 0.8m);

            if (winRate > winRateThreshold)
            {
                riskScore += 50;
                suspiciousFactors.Add($"Abnormal win rate: {winRate:P} (expected: {expectedWinRate:P})");
            }
        }

        var riskThreshold = await _parameterService.GetDecimalParameterAsync("FRAUD_SPIN_RISK_THRESHOLD", 60m);
        if (riskScore >= riskThreshold)
        {
            await ReportSuspiciousActivityAsync(
                "WHEEL_SPIN_FRAUD",
                $"Suspicious wheel spinning detected: {string.Join(", ", suspiciousFactors)}",
                riskScore >= 80 ? SuspiciousActivitySeverity.High : SuspiciousActivitySeverity.Medium,
                playerId,
                "Player",
                null,
                new { CampaignId = campaignId, RiskScore = riskScore, Factors = suspiciousFactors },
                ipAddress,
                userAgent);

            return true;
        }

        return false;
    }

    public async Task<bool> AnalyzeTokenTransactionAsync(Guid playerId, TransactionType type, int amount, string ipAddress)
    {
        var riskScore = 0m;
        var suspiciousFactors = new List<string>();

        // Check transaction frequency
        var recentTransactions = await _context.TokenTransactions
            .Where(tt => tt.PlayerId == playerId && tt.CreatedAt >= DateTime.UtcNow.AddHours(-1))
            .CountAsync();

        var maxTransactionsPerHour = await _parameterService.GetIntParameterAsync("FRAUD_MAX_TRANSACTIONS_PER_HOUR", 100);
        if (recentTransactions > maxTransactionsPerHour)
        {
            riskScore += 35;
            suspiciousFactors.Add($"Excessive transactions: {recentTransactions} in last hour");
        }

        // Check for unusual amounts
        if (type == TransactionType.Purchased && amount > 1000)
        {
            riskScore += 20;
            suspiciousFactors.Add($"Large token purchase: {amount} tokens");
        }

        var riskThreshold = await _parameterService.GetDecimalParameterAsync("FRAUD_TRANSACTION_RISK_THRESHOLD", 40m);
        if (riskScore >= riskThreshold)
        {
            await ReportSuspiciousActivityAsync(
                "TOKEN_TRANSACTION_FRAUD",
                $"Suspicious token transaction detected: {string.Join(", ", suspiciousFactors)}",
                riskScore >= 70 ? SuspiciousActivitySeverity.High : SuspiciousActivitySeverity.Medium,
                playerId,
                "Player",
                null,
                new { Type = type, Amount = amount, RiskScore = riskScore, Factors = suspiciousFactors },
                ipAddress);

            return true;
        }

        return false;
    }

    public async Task<bool> AnalyzeLoginPatternAsync(Guid userId, string userType, string ipAddress, string? userAgent = null)
    {
        var riskScore = 0m;
        var suspiciousFactors = new List<string>();

        // Check login frequency
        var recentLogins = await _context.AuditLogs
            .Where(al => al.UserId == userId && al.Action == "LOGIN_SUCCESS" && al.CreatedAt >= DateTime.UtcNow.AddHours(-1))
            .CountAsync();

        var maxLoginsPerHour = await _parameterService.GetIntParameterAsync("FRAUD_MAX_LOGINS_PER_HOUR", 10);
        if (recentLogins > maxLoginsPerHour)
        {
            riskScore += 30;
            suspiciousFactors.Add($"Excessive logins: {recentLogins} in last hour");
        }

        // Check for failed login attempts
        var failedLogins = await _context.AuditLogs
            .Where(al => al.UserEmail != null && al.Action == "LOGIN_FAILED" && al.CreatedAt >= DateTime.UtcNow.AddHours(-24))
            .CountAsync();

        if (failedLogins > 5)
        {
            riskScore += 25;
            suspiciousFactors.Add($"Multiple failed login attempts: {failedLogins} in last 24 hours");
        }

        var riskThreshold = await _parameterService.GetDecimalParameterAsync("FRAUD_LOGIN_RISK_THRESHOLD", 45m);
        if (riskScore >= riskThreshold)
        {
            await ReportSuspiciousActivityAsync(
                "LOGIN_PATTERN_FRAUD",
                $"Suspicious login pattern detected: {string.Join(", ", suspiciousFactors)}",
                riskScore >= 70 ? SuspiciousActivitySeverity.High : SuspiciousActivitySeverity.Medium,
                userId,
                userType,
                null,
                new { RiskScore = riskScore, Factors = suspiciousFactors },
                ipAddress,
                userAgent);

            return true;
        }

        return false;
    }

    public async Task<bool> AnalyzeAccountCreationAsync(string email, string ipAddress, string? userAgent = null)
    {
        var riskScore = 0m;
        var suspiciousFactors = new List<string>();

        // Check for multiple accounts from same IP
        var accountsFromIp = await _context.AuditLogs
            .Where(al => al.IpAddress == ipAddress && 
                        (al.Action == "PLAYER_CREATED" || al.Action == "BUSINESS_CREATED") && 
                        al.CreatedAt >= DateTime.UtcNow.AddDays(-7))
            .CountAsync();

        var maxAccountsPerIp = await _parameterService.GetIntParameterAsync("FRAUD_MAX_ACCOUNTS_PER_IP", 3);
        if (accountsFromIp > maxAccountsPerIp)
        {
            riskScore += 40;
            suspiciousFactors.Add($"Multiple accounts from IP: {accountsFromIp} in last 7 days");
        }

        // Check for disposable email domains
        var disposableDomains = new[] { "10minutemail.com", "tempmail.org", "guerrillamail.com" };
        var emailDomain = email.Split('@').LastOrDefault()?.ToLower();
        if (emailDomain != null && disposableDomains.Contains(emailDomain))
        {
            riskScore += 30;
            suspiciousFactors.Add("Disposable email domain detected");
        }

        var riskThreshold = await _parameterService.GetDecimalParameterAsync("FRAUD_ACCOUNT_CREATION_RISK_THRESHOLD", 50m);
        if (riskScore >= riskThreshold)
        {
            await ReportSuspiciousActivityAsync(
                "ACCOUNT_CREATION_FRAUD",
                $"Suspicious account creation detected: {string.Join(", ", suspiciousFactors)}",
                riskScore >= 75 ? SuspiciousActivitySeverity.High : SuspiciousActivitySeverity.Medium,
                null,
                "Unknown",
                email,
                new { Email = email, RiskScore = riskScore, Factors = suspiciousFactors },
                ipAddress,
                userAgent);

            return true;
        }

        return false;
    }

    // Risk Scoring
    public async Task<decimal> CalculateUserRiskScoreAsync(Guid userId, string userType)
    {
        var riskScore = 0m;

        // Check account age
        DateTime? createdAt = null;
        if (userType == "Player")
        {
            var player = await _context.Players.FindAsync(userId);
            createdAt = player?.CreatedAt;
        }
        else if (userType == "Business")
        {
            var business = await _context.Businesses.FindAsync(userId);
            createdAt = business?.CreatedAt;
        }

        if (createdAt.HasValue)
        {
            var accountAge = DateTime.UtcNow - createdAt.Value;
            if (accountAge.TotalDays < 1)
                riskScore += 20;
            else if (accountAge.TotalDays < 7)
                riskScore += 10;
        }

        // Check previous suspicious activities
        var suspiciousActivities = await _context.SuspiciousActivities
            .Where(sa => sa.UserId == userId && sa.Status == SuspiciousActivityStatus.Open)
            .CountAsync();

        riskScore += suspiciousActivities * 15;

        return Math.Min(riskScore, 100);
    }

    public async Task<decimal> CalculateLocationRiskScoreAsync(string ipAddress)
    {
        // This is a simplified implementation
        // In a real system, you would use IP geolocation services
        var riskScore = 0m;

        // Check for known VPN/proxy IPs (simplified)
        var knownVpnRanges = new[] { "10.", "192.168.", "172." };
        if (knownVpnRanges.Any(range => ipAddress.StartsWith(range)))
        {
            riskScore += 15;
        }

        return riskScore;
    }

    public async Task<decimal> CalculateDeviceRiskScoreAsync(string? userAgent, string ipAddress)
    {
        var riskScore = 0m;

        if (string.IsNullOrEmpty(userAgent))
        {
            riskScore += 20;
        }
        else if (userAgent.Contains("bot", StringComparison.OrdinalIgnoreCase) ||
                 userAgent.Contains("crawler", StringComparison.OrdinalIgnoreCase))
        {
            riskScore += 40;
        }

        return riskScore;
    }

    // Suspicious Activity Management
    public async Task<SuspiciousActivity> ReportSuspiciousActivityAsync(
        string activityType,
        string description,
        SuspiciousActivitySeverity severity,
        Guid? userId,
        string? userType,
        string? userEmail,
        object activityData,
        string? ipAddress = null,
        string? userAgent = null)
    {
        var riskScore = await CalculateUserRiskScoreAsync(userId ?? Guid.Empty, userType ?? "Unknown");
        
        var activity = SuspiciousActivity.Create(
            activityType,
            description,
            severity,
            userId,
            userType,
            userEmail,
            JsonSerializer.Serialize(activityData),
            riskScore,
            ipAddress,
            userAgent);

        _context.SuspiciousActivities.Add(activity);
        await _context.SaveChangesAsync();

        _logger.LogWarning("Suspicious activity detected: {ActivityType} for user {UserId} with severity {Severity}",
            activityType, userId, severity);

        // Trigger automated response if needed
        await TriggerAutomatedResponseAsync(activity);

        return activity;
    }

    public async Task<IEnumerable<SuspiciousActivity>> GetSuspiciousActivitiesAsync(
        SuspiciousActivityStatus? status = null,
        SuspiciousActivitySeverity? minSeverity = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.SuspiciousActivities
            .Include(sa => sa.ReviewedByAdmin)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(sa => sa.Status == status.Value);

        if (minSeverity.HasValue)
            query = query.Where(sa => sa.Severity >= minSeverity.Value);

        if (startDate.HasValue)
            query = query.Where(sa => sa.DetectedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(sa => sa.DetectedAt <= endDate.Value);

        return await query
            .OrderByDescending(sa => sa.Severity)
            .ThenByDescending(sa => sa.DetectedAt)
            .ToListAsync();
    }

    public async Task<SuspiciousActivity> ReviewSuspiciousActivityAsync(Guid activityId, Guid reviewerId, string notes, string? actionTaken = null)
    {
        var activity = await _context.SuspiciousActivities
            .FirstOrDefaultAsync(sa => sa.Id == activityId);

        if (activity == null)
            throw new ArgumentException("Suspicious activity not found");

        activity.MarkAsReviewed(reviewerId, notes, actionTaken);
        await _context.SaveChangesAsync();

        return activity;
    }

    public async Task<SuspiciousActivity> MarkAsFalsePositiveAsync(Guid activityId, Guid reviewerId, string notes)
    {
        var activity = await _context.SuspiciousActivities
            .FirstOrDefaultAsync(sa => sa.Id == activityId);

        if (activity == null)
            throw new ArgumentException("Suspicious activity not found");

        activity.MarkAsFalsePositive(reviewerId, notes);
        await _context.SaveChangesAsync();

        return activity;
    }

    // Pattern Detection
    public async Task<bool> DetectVelocityAnomaliesAsync(Guid userId, string activityType, TimeSpan timeWindow, int threshold)
    {
        var startTime = DateTime.UtcNow - timeWindow;
        
        var activityCount = activityType.ToUpper() switch
        {
            "QR_SCAN" => await _context.QRSessions.Where(qs => qs.PlayerId == userId && qs.CreatedAt >= startTime).CountAsync(),
            "WHEEL_SPIN" => await _context.WheelSpins.Where(ws => ws.PlayerId == userId && ws.CreatedAt >= startTime).CountAsync(),
            "TOKEN_TRANSACTION" => await _context.TokenTransactions.Where(tt => tt.PlayerId == userId && tt.CreatedAt >= startTime).CountAsync(),
            _ => 0
        };

        return activityCount > threshold;
    }

    public async Task<bool> DetectLocationAnomaliesAsync(Guid userId, string currentLocation, string? previousLocation = null)
    {
        // Simplified implementation - in reality, you'd use proper geolocation
        if (previousLocation == null)
        {
            var lastSession = await _context.QRSessions
                .Where(qs => qs.PlayerId == userId)
                .OrderByDescending(qs => qs.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastSession?.PlayerLocation != null)
            {
                // Check if the locations are significantly different
                // This is a simplified check - real implementation would calculate actual distance
                return !currentLocation.Equals(previousLocation, StringComparison.OrdinalIgnoreCase);
            }
        }

        return false;
    }

    public async Task<bool> DetectDeviceAnomaliesAsync(Guid userId, string currentUserAgent, string? previousUserAgent = null)
    {
        if (previousUserAgent == null)
        {
            var lastAuditLog = await _context.AuditLogs
                .Where(al => al.UserId == userId && !string.IsNullOrEmpty(al.UserAgent))
                .OrderByDescending(al => al.CreatedAt)
                .FirstOrDefaultAsync();

            previousUserAgent = lastAuditLog?.UserAgent;
        }

        if (previousUserAgent != null)
        {
            // Simple device fingerprint comparison
            return !currentUserAgent.Equals(previousUserAgent, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    // Automated Actions
    public async Task<bool> ShouldBlockUserAsync(Guid userId, string userType)
    {
        var criticalActivities = await _context.SuspiciousActivities
            .Where(sa => sa.UserId == userId && 
                        sa.Severity == SuspiciousActivitySeverity.Critical && 
                        sa.Status == SuspiciousActivityStatus.Open)
            .CountAsync();

        var blockThreshold = await _parameterService.GetIntParameterAsync("FRAUD_AUTO_BLOCK_THRESHOLD", 3);
        return criticalActivities >= blockThreshold;
    }

    public async Task<bool> ShouldRequireAdditionalVerificationAsync(Guid userId, string activityType)
    {
        var riskScore = await CalculateUserRiskScoreAsync(userId, "Player");
        var verificationThreshold = await _parameterService.GetDecimalParameterAsync("FRAUD_VERIFICATION_THRESHOLD", 70m);
        
        return riskScore >= verificationThreshold;
    }

    public async Task TriggerAutomatedResponseAsync(SuspiciousActivity activity)
    {
        if (activity.RequiresImmedateAction())
        {
            _logger.LogCritical("Critical suspicious activity detected: {ActivityType} for user {UserId}",
                activity.ActivityType, activity.UserId);

            // In a real system, you might:
            // - Send alerts to administrators
            // - Temporarily suspend the account
            // - Require additional verification
            // - Block certain actions
        }
    }
}