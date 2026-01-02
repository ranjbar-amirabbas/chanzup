namespace Chanzup.Application.DTOs;

public class RedemptionStatsResponse
{
    public Guid BusinessId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    public int TotalPrizesAwarded { get; set; }
    public int TotalPrizesRedeemed { get; set; }
    public int TotalPrizesExpired { get; set; }
    public int PendingRedemptions { get; set; }
    
    public decimal RedemptionRate { get; set; }
    public decimal ExpirationRate { get; set; }
    
    public decimal TotalPrizeValue { get; set; }
    public decimal RedeemedPrizeValue { get; set; }
    public decimal ExpiredPrizeValue { get; set; }
    
    public IEnumerable<PrizeRedemptionStat> PrizeBreakdown { get; set; } = new List<PrizeRedemptionStat>();
}

public class PrizeRedemptionStat
{
    public Guid PrizeId { get; set; }
    public string PrizeName { get; set; } = string.Empty;
    public int Awarded { get; set; }
    public int Redeemed { get; set; }
    public int Expired { get; set; }
    public decimal RedemptionRate { get; set; }
}