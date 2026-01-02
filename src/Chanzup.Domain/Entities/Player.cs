using Chanzup.Domain.ValueObjects;

namespace Chanzup.Domain.Entities;

public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Email Email { get; set; } = new("placeholder@example.com");
    public string PasswordHash { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public int TokenBalance { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<QRSession> QRSessions { get; set; } = new List<QRSession>();
    public ICollection<WheelSpin> WheelSpins { get; set; } = new List<WheelSpin>();
    public ICollection<PlayerPrize> PlayerPrizes { get; set; } = new List<PlayerPrize>();
    public ICollection<TokenTransaction> TokenTransactions { get; set; } = new List<TokenTransaction>();
    public ICollection<Referral> ReferralsMade { get; set; } = new List<Referral>();
    public ICollection<Referral> ReferralsReceived { get; set; } = new List<Referral>();
    public ICollection<SocialShare> SocialShares { get; set; } = new List<SocialShare>();

    // Domain methods
    public bool CanAffordSpin(int tokenCost)
    {
        return IsActive && TokenBalance >= tokenCost;
    }

    public void AddTokens(int amount, string description)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        TokenBalance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SpendTokens(int amount, string description)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        if (TokenBalance < amount)
            throw new InvalidOperationException("Insufficient token balance");

        TokenBalance -= amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
            return $"{FirstName} {LastName}";
        
        if (!string.IsNullOrEmpty(FirstName))
            return FirstName;

        return Email.Value.Split('@')[0];
    }
}