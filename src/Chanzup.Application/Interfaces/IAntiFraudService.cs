using Chanzup.Application.DTOs;

namespace Chanzup.Application.Interfaces;

public interface IAntiFraudService
{
    /// <summary>
    /// Validates if a QR scan request is legitimate
    /// </summary>
    Task<AntiFraudResult> ValidateQRScanAsync(Guid playerId, QRScanRequest request);

    /// <summary>
    /// Checks for replay attacks using session hash
    /// </summary>
    Task<bool> IsReplayAttackAsync(string sessionHash);

    /// <summary>
    /// Validates player movement patterns for impossible travel
    /// </summary>
    Task<bool> ValidateMovementPatternAsync(Guid playerId, decimal latitude, decimal longitude, DateTime timestamp);

    /// <summary>
    /// Checks if player behavior is suspicious
    /// </summary>
    Task<bool> IsSuspiciousBehaviorAsync(Guid playerId);

    /// <summary>
    /// Records a suspicious activity
    /// </summary>
    Task RecordSuspiciousActivityAsync(Guid playerId, string activityType, string details);
}

public class AntiFraudResult
{
    public bool IsValid { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsSuspicious { get; set; }
}