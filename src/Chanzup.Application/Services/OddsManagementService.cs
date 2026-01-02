using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chanzup.Application.Services;

public class OddsManagementService : IOddsManagementService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<OddsManagementService> _logger;

    // Configuration constants
    private const decimal MinimumOdds = 0.001m; // 0.1% minimum odds
    private const decimal MaximumOdds = 0.8m;   // 80% maximum odds for any single prize
    private const int CriticalInventoryThreshold = 5; // Items remaining before critical adjustment
    private const int LowInventoryThreshold = 20; // Items remaining before low inventory adjustment

    public OddsManagementService(
        IApplicationDbContext context,
        ILogger<OddsManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RecalculateOddsAsync(Guid campaignId)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.Prizes)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign == null)
        {
            _logger.LogWarning("Campaign {CampaignId} not found for odds recalculation", campaignId);
            return;
        }

        var availablePrizes = campaign.Prizes.Where(p => p.IsActive).ToList();
        if (!availablePrizes.Any())
        {
            _logger.LogInformation("No active prizes found for campaign {CampaignId}", campaignId);
            return;
        }

        var adjustments = new List<PrizeOddsAdjustment>();
        var totalOriginalOdds = availablePrizes.Sum(p => p.WinProbability);

        foreach (var prize in availablePrizes)
        {
            var originalOdds = prize.WinProbability;
            var adjustedOdds = CalculateAdjustedOdds(prize, totalOriginalOdds);

            if (Math.Abs(originalOdds - adjustedOdds) > 0.001m)
            {
                adjustments.Add(new PrizeOddsAdjustment
                {
                    PrizeId = prize.Id,
                    PrizeName = prize.Name,
                    CurrentOdds = originalOdds,
                    RecommendedOdds = adjustedOdds,
                    Reason = GetAdjustmentReason(prize),
                    RemainingQuantity = prize.RemainingQuantity,
                    TotalQuantity = prize.TotalQuantity
                });

                // Note: We don't automatically update the odds in the database
                // This service provides recommendations that can be applied manually or through business rules
                _logger.LogInformation("Odds adjustment recommended for prize {PrizeId}: {Original} -> {Adjusted}", 
                    prize.Id, originalOdds, adjustedOdds);
            }
        }

        if (adjustments.Any())
        {
            _logger.LogInformation("Recommended {Count} odds adjustments for campaign {CampaignId}", 
                adjustments.Count, campaignId);
        }
    }

    public async Task<Dictionary<Guid, decimal>> GetEffectiveOddsAsync(Guid campaignId)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.Prizes)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign == null)
            return new Dictionary<Guid, decimal>();

        var availablePrizes = campaign.Prizes.Where(p => p.IsAvailable()).ToList();
        var effectiveOdds = new Dictionary<Guid, decimal>();
        var totalOriginalOdds = availablePrizes.Sum(p => p.WinProbability);

        foreach (var prize in availablePrizes)
        {
            var adjustedOdds = CalculateAdjustedOdds(prize, totalOriginalOdds);
            effectiveOdds[prize.Id] = adjustedOdds;
        }

        // Normalize odds to ensure they sum to a reasonable total (not exceeding 1.0)
        var totalEffectiveOdds = effectiveOdds.Values.Sum();
        if (totalEffectiveOdds > 1.0m)
        {
            var normalizationFactor = 0.95m / totalEffectiveOdds; // Keep total under 95%
            var normalizedOdds = new Dictionary<Guid, decimal>();
            
            foreach (var kvp in effectiveOdds)
            {
                normalizedOdds[kvp.Key] = kvp.Value * normalizationFactor;
            }
            
            return normalizedOdds;
        }

        return effectiveOdds;
    }

    public async Task HandlePrizeDepletionAsync(Guid prizeId)
    {
        var prize = await _context.Prizes
            .Include(p => p.Campaign)
            .FirstOrDefaultAsync(p => p.Id == prizeId);

        if (prize == null)
        {
            _logger.LogWarning("Prize {PrizeId} not found for depletion handling", prizeId);
            return;
        }

        if (prize.RemainingQuantity <= 0)
        {
            // Prize is completely depleted - deactivate it
            prize.IsActive = false;
            prize.UpdatedAt = DateTime.UtcNow;
            
            _logger.LogInformation("Prize {PrizeId} ({Name}) has been deactivated due to depletion", 
                prizeId, prize.Name);

            // Recalculate odds for the entire campaign
            await RecalculateOddsAsync(prize.CampaignId);
        }
        else if (prize.RemainingQuantity <= CriticalInventoryThreshold)
        {
            // Critical inventory level - log warning and suggest odds adjustment
            _logger.LogWarning("Prize {PrizeId} ({Name}) has critical inventory: {Remaining} remaining", 
                prizeId, prize.Name, prize.RemainingQuantity);

            await RecalculateOddsAsync(prize.CampaignId);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> ValidateCampaignOddsAsync(Guid campaignId)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.Prizes)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign == null)
            return false;

        var activePrizes = campaign.Prizes.Where(p => p.IsActive).ToList();
        
        // Check if there are any active prizes
        if (!activePrizes.Any())
        {
            _logger.LogWarning("Campaign {CampaignId} has no active prizes", campaignId);
            return false;
        }

        // Check if any prizes have available inventory
        var availablePrizes = activePrizes.Where(p => p.IsAvailable()).ToList();
        if (!availablePrizes.Any())
        {
            _logger.LogWarning("Campaign {CampaignId} has no available prizes", campaignId);
            return false;
        }

        // Check total odds don't exceed reasonable limits
        var totalOdds = activePrizes.Sum(p => p.WinProbability);
        if (totalOdds > 1.0m)
        {
            _logger.LogWarning("Campaign {CampaignId} has total odds exceeding 100%: {TotalOdds}", 
                campaignId, totalOdds);
            return false;
        }

        // Check individual prize odds are within reasonable bounds
        foreach (var prize in activePrizes)
        {
            if (prize.WinProbability < MinimumOdds || prize.WinProbability > MaximumOdds)
            {
                _logger.LogWarning("Prize {PrizeId} has odds outside acceptable range: {Odds}", 
                    prize.Id, prize.WinProbability);
                return false;
            }
        }

        return true;
    }

    public async Task<OddsRecommendation> GetOddsRecommendationAsync(Guid campaignId)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.Prizes)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        var recommendation = new OddsRecommendation
        {
            CampaignId = campaignId,
            RequiresAdjustment = false,
            Reason = "No adjustments needed"
        };

        if (campaign == null)
        {
            recommendation.Reason = "Campaign not found";
            return recommendation;
        }

        var availablePrizes = campaign.Prizes.Where(p => p.IsAvailable()).ToList();
        if (!availablePrizes.Any())
        {
            recommendation.RequiresAdjustment = true;
            recommendation.Reason = "No available prizes - campaign needs new inventory or prize activation";
            return recommendation;
        }

        var totalOriginalOdds = availablePrizes.Sum(p => p.WinProbability);
        var adjustments = new List<PrizeOddsAdjustment>();

        foreach (var prize in availablePrizes)
        {
            var adjustedOdds = CalculateAdjustedOdds(prize, totalOriginalOdds);
            
            if (Math.Abs(prize.WinProbability - adjustedOdds) > 0.001m)
            {
                adjustments.Add(new PrizeOddsAdjustment
                {
                    PrizeId = prize.Id,
                    PrizeName = prize.Name,
                    CurrentOdds = prize.WinProbability,
                    RecommendedOdds = adjustedOdds,
                    Reason = GetAdjustmentReason(prize),
                    RemainingQuantity = prize.RemainingQuantity,
                    TotalQuantity = prize.TotalQuantity
                });
            }
        }

        if (adjustments.Any())
        {
            recommendation.RequiresAdjustment = true;
            recommendation.Reason = $"Inventory levels require odds adjustment for {adjustments.Count} prizes";
            recommendation.Adjustments = adjustments;
        }

        return recommendation;
    }

    private decimal CalculateAdjustedOdds(Prize prize, decimal totalOriginalOdds)
    {
        if (prize.TotalQuantity <= 0)
            return prize.WinProbability;

        var inventoryPercentage = (decimal)prize.RemainingQuantity / prize.TotalQuantity;
        var adjustmentFactor = CalculateInventoryAdjustmentFactor(inventoryPercentage, prize.RemainingQuantity);
        
        var adjustedOdds = prize.WinProbability * adjustmentFactor;
        
        // Ensure odds stay within acceptable bounds
        return Math.Max(MinimumOdds, Math.Min(MaximumOdds, adjustedOdds));
    }

    private decimal CalculateInventoryAdjustmentFactor(decimal inventoryPercentage, int remainingQuantity)
    {
        // Critical inventory (≤5 items): Reduce odds significantly
        if (remainingQuantity <= CriticalInventoryThreshold)
        {
            return 0.2m; // Reduce to 20% of original odds
        }

        // Low inventory (≤20 items): Reduce odds moderately
        if (remainingQuantity <= LowInventoryThreshold)
        {
            return 0.5m; // Reduce to 50% of original odds
        }

        // Normal inventory adjustment based on percentage
        // 100% inventory: factor = 1.0
        // 75% inventory: factor = 0.9
        // 50% inventory: factor = 0.8
        // 25% inventory: factor = 0.7
        return Math.Max(0.2m, 0.6m + (inventoryPercentage * 0.4m));
    }

    private string GetAdjustmentReason(Prize prize)
    {
        var inventoryPercentage = prize.TotalQuantity > 0 
            ? (decimal)prize.RemainingQuantity / prize.TotalQuantity * 100 
            : 0;

        if (prize.RemainingQuantity <= 0)
            return "Prize depleted - should be deactivated";
        
        if (prize.RemainingQuantity <= CriticalInventoryThreshold)
            return $"Critical inventory level ({prize.RemainingQuantity} remaining)";
        
        if (prize.RemainingQuantity <= LowInventoryThreshold)
            return $"Low inventory level ({prize.RemainingQuantity} remaining)";
        
        if (inventoryPercentage < 50)
            return $"Inventory below 50% ({inventoryPercentage:F1}% remaining)";
        
        return $"Inventory adjustment ({inventoryPercentage:F1}% remaining)";
    }
}