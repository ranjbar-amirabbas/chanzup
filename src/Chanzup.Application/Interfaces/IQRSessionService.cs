using Chanzup.Application.DTOs;

namespace Chanzup.Application.Interfaces;

public interface IQRSessionService
{
    /// <summary>
    /// Processes a QR code scan and creates a session
    /// </summary>
    Task<QRScanResponse> ProcessQRScanAsync(Guid playerId, QRScanRequest request);

    /// <summary>
    /// Validates if a player can scan at a business location
    /// </summary>
    Task<bool> CanPlayerScanAsync(Guid playerId, Guid businessId, DateTime? scanTime = null);

    /// <summary>
    /// Gets the remaining spins for a player for a specific campaign today
    /// </summary>
    Task<int> GetRemainingSpinsTodayAsync(Guid playerId, Guid campaignId);

    /// <summary>
    /// Validates player location against business location
    /// </summary>
    Task<bool> ValidateLocationAsync(Guid businessId, decimal playerLatitude, decimal playerLongitude);

    /// <summary>
    /// Checks if player is within cooldown period for a business
    /// </summary>
    Task<bool> IsWithinCooldownPeriodAsync(Guid playerId, Guid businessId, DateTime? scanTime = null);

    /// <summary>
    /// Gets daily token earning limit for a player at a business
    /// </summary>
    Task<int> GetDailyTokenLimitAsync(Guid playerId, Guid businessId);

    /// <summary>
    /// Gets tokens earned today by player at a business
    /// </summary>
    Task<int> GetTokensEarnedTodayAsync(Guid playerId, Guid businessId);
}