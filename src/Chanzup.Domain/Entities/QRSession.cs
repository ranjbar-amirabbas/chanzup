using Chanzup.Domain.ValueObjects;

namespace Chanzup.Domain.Entities;

public class QRSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlayerId { get; set; }
    public Guid BusinessId { get; set; }
    public Location? PlayerLocation { get; set; }
    public int TokensEarned { get; set; } = 0;
    public string SessionHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Player Player { get; set; } = null!;
    public Business Business { get; set; } = null!;

    // Domain methods
    public bool IsValid(TimeSpan validityPeriod)
    {
        return DateTime.UtcNow - CreatedAt <= validityPeriod;
    }

    public bool IsWithinCooldownPeriod(TimeSpan cooldownPeriod)
    {
        return DateTime.UtcNow - CreatedAt < cooldownPeriod;
    }

    public void UpdateLocation(decimal latitude, decimal longitude)
    {
        PlayerLocation = new Location(latitude, longitude);
    }

    public static string GenerateSessionHash(Guid playerId, Guid businessId, DateTime timestamp)
    {
        var input = $"{playerId}:{businessId}:{timestamp:yyyy-MM-dd HH:mm:ss}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(input));
    }
}