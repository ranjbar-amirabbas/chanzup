using Chanzup.Domain.Entities;

namespace Chanzup.Application.Interfaces;

public interface IOddsManagementService
{
    /// <summary>
    /// Recalculates and updates prize odds based on current inventory levels
    /// </summary>
    Task RecalculateOddsAsync(Guid campaignId);

    /// <summary>
    /// Gets the current effective odds for all prizes in a campaign
    /// </summary>
    Task<Dictionary<Guid, decimal>> GetEffectiveOddsAsync(Guid campaignId);

    /// <summary>
    /// Handles prize depletion by adjusting odds or disabling prizes
    /// </summary>
    Task HandlePrizeDepletionAsync(Guid prizeId);

    /// <summary>
    /// Validates that campaign odds are properly configured
    /// </summary>
    Task<bool> ValidateCampaignOddsAsync(Guid campaignId);

    /// <summary>
    /// Gets odds adjustment recommendations for a campaign
    /// </summary>
    Task<OddsRecommendation> GetOddsRecommendationAsync(Guid campaignId);
}

public class OddsRecommendation
{
    public Guid CampaignId { get; set; }
    public bool RequiresAdjustment { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<PrizeOddsAdjustment> Adjustments { get; set; } = new();
}

public class PrizeOddsAdjustment
{
    public Guid PrizeId { get; set; }
    public string PrizeName { get; set; } = string.Empty;
    public decimal CurrentOdds { get; set; }
    public decimal RecommendedOdds { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int RemainingQuantity { get; set; }
    public int TotalQuantity { get; set; }
}