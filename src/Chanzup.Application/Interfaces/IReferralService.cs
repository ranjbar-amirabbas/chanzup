using Chanzup.Domain.Entities;

namespace Chanzup.Application.Interfaces;

public interface IReferralService
{
    /// <summary>
    /// Generates a unique referral code for a player
    /// </summary>
    Task<string> GenerateReferralCodeAsync(Guid playerId);

    /// <summary>
    /// Creates a referral record when a player uses a referral code
    /// </summary>
    Task<Referral> UseReferralCodeAsync(string referralCode, Guid newPlayerId);

    /// <summary>
    /// Completes a referral and awards tokens to both referrer and referred player
    /// </summary>
    Task<bool> CompleteReferralAsync(Guid referralId);

    /// <summary>
    /// Gets all referrals made by a player
    /// </summary>
    Task<IEnumerable<Referral>> GetPlayerReferralsAsync(Guid playerId);

    /// <summary>
    /// Gets referral statistics for a player
    /// </summary>
    Task<ReferralStats> GetReferralStatsAsync(Guid playerId);

    /// <summary>
    /// Validates if a referral code is valid and available
    /// </summary>
    Task<bool> IsReferralCodeValidAsync(string referralCode);

    /// <summary>
    /// Records a social share and awards tokens if eligible
    /// </summary>
    Task<SocialShare> RecordSocialShareAsync(Guid playerId, SocialPlatform platform, string content, string? externalShareId = null);

    /// <summary>
    /// Verifies a social share and awards tokens
    /// </summary>
    Task<bool> VerifySocialShareAsync(Guid socialShareId);

    /// <summary>
    /// Gets social sharing history for a player
    /// </summary>
    Task<IEnumerable<SocialShare>> GetPlayerSocialSharesAsync(Guid playerId);

    /// <summary>
    /// Gets daily social sharing count for a player
    /// </summary>
    Task<int> GetDailySocialShareCountAsync(Guid playerId, DateTime date);
}

public class ReferralStats
{
    public int TotalReferrals { get; set; }
    public int CompletedReferrals { get; set; }
    public int PendingReferrals { get; set; }
    public int TotalTokensEarned { get; set; }
    public DateTime? LastReferralDate { get; set; }
}