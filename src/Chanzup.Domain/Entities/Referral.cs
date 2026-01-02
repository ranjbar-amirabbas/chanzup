namespace Chanzup.Domain.Entities;

public class Referral
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReferrerId { get; set; }
    public Guid ReferredPlayerId { get; set; }
    public string ReferralCode { get; set; } = string.Empty;
    public int TokensAwarded { get; set; }
    public bool IsCompleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public Player Referrer { get; set; } = null!;
    public Player ReferredPlayer { get; set; } = null!;

    // Domain methods
    public void CompleteReferral(int tokenReward)
    {
        if (IsCompleted)
            throw new InvalidOperationException("Referral is already completed");

        IsCompleted = true;
        TokensAwarded = tokenReward;
        CompletedAt = DateTime.UtcNow;
    }

    public bool IsEligibleForCompletion()
    {
        return !IsCompleted && ReferredPlayerId != Guid.Empty;
    }

    public static Referral Create(Guid referrerId, string referralCode)
    {
        return new Referral
        {
            ReferrerId = referrerId,
            ReferralCode = referralCode,
            CreatedAt = DateTime.UtcNow
        };
    }
}