namespace Chanzup.Domain.Entities;

public class TokenTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlayerId { get; set; }
    public TransactionType Type { get; set; }
    public int Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? RelatedEntityId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Player Player { get; set; } = null!;

    // Domain methods
    public bool IsCredit()
    {
        return Type == TransactionType.Earned || 
               Type == TransactionType.Purchased || 
               Type == TransactionType.Bonus;
    }

    public bool IsDebit()
    {
        return Type == TransactionType.Spent;
    }

    public int GetSignedAmount()
    {
        return IsCredit() ? Amount : -Amount;
    }

    public static TokenTransaction CreateEarned(Guid playerId, int amount, string description, Guid? relatedEntityId = null)
    {
        return new TokenTransaction
        {
            PlayerId = playerId,
            Type = TransactionType.Earned,
            Amount = amount,
            Description = description,
            RelatedEntityId = relatedEntityId
        };
    }

    public static TokenTransaction CreateSpent(Guid playerId, int amount, string description, Guid? relatedEntityId = null)
    {
        return new TokenTransaction
        {
            PlayerId = playerId,
            Type = TransactionType.Spent,
            Amount = amount,
            Description = description,
            RelatedEntityId = relatedEntityId
        };
    }
}

public enum TransactionType
{
    Earned = 0,
    Spent = 1,
    Purchased = 2,
    Bonus = 3
}