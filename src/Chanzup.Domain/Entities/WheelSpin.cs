namespace Chanzup.Domain.Entities;

public class WheelSpin
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlayerId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid? PrizeId { get; set; }
    public int TokensSpent { get; set; }
    public string SpinResult { get; set; } = string.Empty;
    public string RandomSeed { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Player Player { get; set; } = null!;
    public Campaign Campaign { get; set; } = null!;
    public Prize? Prize { get; set; }

    // Domain methods
    public bool WonPrize()
    {
        return PrizeId.HasValue && Prize != null;
    }

    public bool IsValidSpin()
    {
        return TokensSpent > 0 && !string.IsNullOrEmpty(RandomSeed);
    }

    public static string GenerateRandomSeed()
    {
        return Guid.NewGuid().ToString("N");
    }
}