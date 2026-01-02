namespace Chanzup.Application.DTOs;

public class PlayerPrizeResponse
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Guid PrizeId { get; set; }
    public string RedemptionCode { get; set; } = string.Empty;
    public bool IsRedeemed { get; set; }
    public DateTime? RedeemedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Prize details
    public string PrizeName { get; set; } = string.Empty;
    public string? PrizeDescription { get; set; }
    public decimal? PrizeValue { get; set; }
    
    // Business details
    public string BusinessName { get; set; } = string.Empty;
    public string? BusinessAddress { get; set; }
    
    // Player details (for staff verification)
    public string? PlayerName { get; set; }
    public string? PlayerEmail { get; set; }
    
    // Computed properties
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool CanBeRedeemed => !IsRedeemed && !IsExpired;
    public TimeSpan TimeUntilExpiry => ExpiresAt - DateTime.UtcNow;
}