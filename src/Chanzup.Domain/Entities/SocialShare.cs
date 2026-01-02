namespace Chanzup.Domain.Entities;

public class SocialShare
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlayerId { get; set; }
    public SocialPlatform Platform { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ExternalShareId { get; set; }
    public int TokensAwarded { get; set; }
    public bool IsVerified { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAt { get; set; }

    // Navigation properties
    public Player Player { get; set; } = null!;

    // Domain methods
    public void VerifyShare(int tokenReward)
    {
        if (IsVerified)
            throw new InvalidOperationException("Share is already verified");

        IsVerified = true;
        TokensAwarded = tokenReward;
        VerifiedAt = DateTime.UtcNow;
    }

    public bool IsEligibleForVerification()
    {
        return !IsVerified && !string.IsNullOrEmpty(Content);
    }

    public static SocialShare Create(Guid playerId, SocialPlatform platform, string content, string? externalShareId = null)
    {
        return new SocialShare
        {
            PlayerId = playerId,
            Platform = platform,
            Content = content,
            ExternalShareId = externalShareId,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public enum SocialPlatform
{
    Facebook = 0,
    Twitter = 1,
    Instagram = 2,
    LinkedIn = 3,
    TikTok = 4,
    Other = 99
}