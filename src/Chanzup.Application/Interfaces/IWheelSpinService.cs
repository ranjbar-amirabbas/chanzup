using Chanzup.Application.DTOs;
using Chanzup.Domain.Entities;

namespace Chanzup.Application.Interfaces;

public interface IWheelSpinService
{
    /// <summary>
    /// Processes a complete wheel spin transaction
    /// </summary>
    Task<WheelSpinResult> ProcessSpinAsync(Guid playerId, Guid campaignId, string sessionId);

    /// <summary>
    /// Validates if a player can spin the wheel
    /// </summary>
    Task<bool> CanPlayerSpinAsync(Guid playerId, Guid campaignId);

    /// <summary>
    /// Gets the player's remaining spins for today
    /// </summary>
    Task<int> GetRemainingSpinsAsync(Guid playerId, Guid campaignId);
}

public class WheelSpinResult
{
    public Guid SpinId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Prize? PrizeWon { get; set; }
    public int TokensSpent { get; set; }
    public int NewTokenBalance { get; set; }
    public string RandomSeed { get; set; } = string.Empty;
    public DateTime SpinTime { get; set; }
}