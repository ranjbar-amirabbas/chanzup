using Chanzup.Domain.ValueObjects;

namespace Chanzup.Domain.Entities;

public class PlayerPrize
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlayerId { get; set; }
    public Guid PrizeId { get; set; }
    public RedemptionCode RedemptionCode { get; set; } = RedemptionCode.Generate();
    public bool IsRedeemed { get; set; } = false;
    public DateTime? RedeemedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Player Player { get; set; } = null!;
    public Prize Prize { get; set; } = null!;

    // Domain methods
    public bool IsExpired()
    {
        return DateTime.UtcNow > ExpiresAt;
    }

    public bool CanBeRedeemed()
    {
        return !IsRedeemed && !IsExpired();
    }

    public void Redeem()
    {
        if (!CanBeRedeemed())
            throw new InvalidOperationException("Prize cannot be redeemed");

        IsRedeemed = true;
        RedeemedAt = DateTime.UtcNow;
    }

    public TimeSpan GetTimeUntilExpiry()
    {
        return ExpiresAt - DateTime.UtcNow;
    }
}