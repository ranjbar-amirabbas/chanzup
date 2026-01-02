using Chanzup.Domain.Entities;

namespace Chanzup.Application.Interfaces;

public interface IFraudDetectionService
{
    // Suspicious Activity Detection
    Task<bool> AnalyzeQRScanAsync(Guid playerId, Guid businessId, string ipAddress, string? userAgent = null);
    Task<bool> AnalyzeWheelSpinAsync(Guid playerId, Guid campaignId, string ipAddress, string? userAgent = null);
    Task<bool> AnalyzeTokenTransactionAsync(Guid playerId, TransactionType type, int amount, string ipAddress);
    Task<bool> AnalyzeLoginPatternAsync(Guid userId, string userType, string ipAddress, string? userAgent = null);
    Task<bool> AnalyzeAccountCreationAsync(string email, string ipAddress, string? userAgent = null);

    // Risk Scoring
    Task<decimal> CalculateUserRiskScoreAsync(Guid userId, string userType);
    Task<decimal> CalculateLocationRiskScoreAsync(string ipAddress);
    Task<decimal> CalculateDeviceRiskScoreAsync(string? userAgent, string ipAddress);

    // Suspicious Activity Management
    Task<SuspiciousActivity> ReportSuspiciousActivityAsync(
        string activityType,
        string description,
        SuspiciousActivitySeverity severity,
        Guid? userId,
        string? userType,
        string? userEmail,
        object activityData,
        string? ipAddress = null,
        string? userAgent = null);

    Task<IEnumerable<SuspiciousActivity>> GetSuspiciousActivitiesAsync(
        SuspiciousActivityStatus? status = null,
        SuspiciousActivitySeverity? minSeverity = null,
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<SuspiciousActivity> ReviewSuspiciousActivityAsync(Guid activityId, Guid reviewerId, string notes, string? actionTaken = null);
    Task<SuspiciousActivity> MarkAsFalsePositiveAsync(Guid activityId, Guid reviewerId, string notes);

    // Pattern Detection
    Task<bool> DetectVelocityAnomaliesAsync(Guid userId, string activityType, TimeSpan timeWindow, int threshold);
    Task<bool> DetectLocationAnomaliesAsync(Guid userId, string currentLocation, string? previousLocation = null);
    Task<bool> DetectDeviceAnomaliesAsync(Guid userId, string currentUserAgent, string? previousUserAgent = null);

    // Automated Actions
    Task<bool> ShouldBlockUserAsync(Guid userId, string userType);
    Task<bool> ShouldRequireAdditionalVerificationAsync(Guid userId, string activityType);
    Task TriggerAutomatedResponseAsync(SuspiciousActivity activity);
}