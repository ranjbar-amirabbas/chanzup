using Microsoft.EntityFrameworkCore;
using Chanzup.Application.DTOs;
using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;

namespace Chanzup.Application.Services;

public class AntiFraudService : IAntiFraudService
{
    private readonly IApplicationDbContext _context;
    private const double MAX_TRAVEL_SPEED_KMH = 100.0; // Maximum reasonable travel speed
    private const int MAX_SCANS_PER_HOUR = 20;
    private const int SUSPICIOUS_ACTIVITY_THRESHOLD = 5;

    public AntiFraudService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AntiFraudResult> ValidateQRScanAsync(Guid playerId, QRScanRequest request)
    {
        // Check for replay attacks
        var sessionHash = QRSession.GenerateSessionHash(playerId, Guid.Empty, request.Timestamp);
        if (await IsReplayAttackAsync(sessionHash))
        {
            await RecordSuspiciousActivityAsync(playerId, "ReplayAttack", $"Attempted replay attack with hash: {sessionHash}");
            return new AntiFraudResult
            {
                IsValid = false,
                Reason = "Duplicate scan detected",
                IsSuspicious = true
            };
        }

        // Validate movement patterns
        if (!await ValidateMovementPatternAsync(playerId, request.Latitude, request.Longitude, request.Timestamp))
        {
            await RecordSuspiciousActivityAsync(playerId, "ImpossibleTravel", $"Impossible travel detected to location: {request.Latitude}, {request.Longitude}");
            return new AntiFraudResult
            {
                IsValid = false,
                Reason = "Impossible travel detected",
                IsSuspicious = true
            };
        }

        // Check for excessive scanning frequency
        var recentScans = await GetRecentScansAsync(playerId, TimeSpan.FromHours(1));
        if (recentScans >= MAX_SCANS_PER_HOUR)
        {
            await RecordSuspiciousActivityAsync(playerId, "ExcessiveScanning", $"Excessive scanning: {recentScans} scans in the last hour");
            return new AntiFraudResult
            {
                IsValid = false,
                Reason = "Too many scans in a short period",
                IsSuspicious = true
            };
        }

        // Check overall suspicious behavior
        var isSuspicious = await IsSuspiciousBehaviorAsync(playerId);
        if (isSuspicious)
        {
            return new AntiFraudResult
            {
                IsValid = false,
                Reason = "Account flagged for suspicious activity",
                IsSuspicious = true
            };
        }

        return new AntiFraudResult
        {
            IsValid = true,
            IsSuspicious = false
        };
    }

    public async Task<bool> IsReplayAttackAsync(string sessionHash)
    {
        var existingSession = await _context.QRSessions
            .FirstOrDefaultAsync(qs => qs.SessionHash == sessionHash);

        return existingSession != null;
    }

    public async Task<bool> ValidateMovementPatternAsync(Guid playerId, decimal latitude, decimal longitude, DateTime timestamp)
    {
        // Get the most recent QR session for this player
        var lastSession = await _context.QRSessions
            .Where(qs => qs.PlayerId == playerId && qs.PlayerLocation != null)
            .OrderByDescending(qs => qs.CreatedAt)
            .FirstOrDefaultAsync();

        if (lastSession?.PlayerLocation == null)
        {
            return true; // No previous location to compare
        }

        var timeDifference = timestamp - lastSession.CreatedAt;
        if (timeDifference.TotalMinutes < 1) // Less than 1 minute
        {
            return true; // Too short to validate travel
        }

        var distance = CalculateDistance(
            (double)lastSession.PlayerLocation.Latitude, (double)lastSession.PlayerLocation.Longitude,
            (double)latitude, (double)longitude);

        var timeHours = timeDifference.TotalHours;
        var speedKmh = distance / 1000.0 / timeHours; // Convert to km/h

        return speedKmh <= MAX_TRAVEL_SPEED_KMH;
    }

    public async Task<bool> IsSuspiciousBehaviorAsync(Guid playerId)
    {
        var recentSuspiciousActivities = await GetRecentSuspiciousActivitiesAsync(playerId, TimeSpan.FromDays(7));
        return recentSuspiciousActivities >= SUSPICIOUS_ACTIVITY_THRESHOLD;
    }

    public async Task RecordSuspiciousActivityAsync(Guid playerId, string activityType, string details)
    {
        // For now, we'll log this. In a real implementation, you might want to store this in a dedicated table
        // or send to a logging service
        await Task.CompletedTask;
        
        // TODO: Implement proper suspicious activity logging
        // This could be stored in a SuspiciousActivity table or sent to a monitoring service
        Console.WriteLine($"Suspicious Activity - Player: {playerId}, Type: {activityType}, Details: {details}");
    }

    private async Task<int> GetRecentScansAsync(Guid playerId, TimeSpan timeWindow)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
        
        return await _context.QRSessions
            .CountAsync(qs => qs.PlayerId == playerId && qs.CreatedAt > cutoffTime);
    }

    private async Task<int> GetRecentSuspiciousActivitiesAsync(Guid playerId, TimeSpan timeWindow)
    {
        // This would query a SuspiciousActivity table in a real implementation
        // For now, return 0 as we don't have persistent storage for suspicious activities
        await Task.CompletedTask;
        return 0;
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