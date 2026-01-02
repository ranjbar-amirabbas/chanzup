using Chanzup.Application.DTOs;

namespace Chanzup.Application.Interfaces;

public interface IRedemptionService
{
    Task<RedemptionVerificationResponse> VerifyRedemptionCodeAsync(string redemptionCode);
    Task<RedemptionCompletionResponse> CompleteRedemptionAsync(string redemptionCode, Guid staffId);
    Task<IEnumerable<PlayerPrizeResponse>> GetPlayerPrizesAsync(Guid playerId, bool includeRedeemed = false);
    Task<int> CleanupExpiredPrizesAsync();
    Task<RedemptionStatsResponse> GetRedemptionStatsAsync(Guid businessId, DateTime? startDate = null, DateTime? endDate = null);
}