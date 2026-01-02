using System.Security.Cryptography;
using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;
using Chanzup.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Chanzup.Application.Services;

public class GameEngineService : IGameEngineService, IDisposable
{
    private readonly IApplicationDbContext _context;
    private readonly IOddsManagementService _oddsManagement;
    private readonly ILogger<GameEngineService> _logger;
    private readonly RandomNumberGenerator _rng;

    public GameEngineService(
        IApplicationDbContext context,
        IOddsManagementService oddsManagement,
        ILogger<GameEngineService> logger)
    {
        _context = context;
        _oddsManagement = oddsManagement;
        _logger = logger;
        _rng = RandomNumberGenerator.Create();
    }

    public async Task<Prize?> SpinWheel(Campaign campaign, Player player)
    {
        if (!CanPlayerSpin(campaign, player))
        {
            _logger.LogWarning("Player {PlayerId} cannot spin wheel for campaign {CampaignId}", 
                player.Id, campaign.Id);
            return null;
        }

        var availablePrizes = campaign.GetAvailablePrizes().ToList();
        if (!availablePrizes.Any())
        {
            _logger.LogInformation("No available prizes for campaign {CampaignId}", campaign.Id);
            return null;
        }

        // Generate cryptographically secure random number
        var randomValue = GenerateSecureRandomDouble();
        
        // Select prize based on probabilities and inventory
        var selectedPrize = await SelectPrizeByProbabilityAsync(availablePrizes, randomValue, campaign.Id);
        
        _logger.LogInformation("Wheel spin for player {PlayerId} in campaign {CampaignId}: {Result}", 
            player.Id, campaign.Id, selectedPrize?.Name ?? "No Prize");

        return selectedPrize;
    }

    public bool CanPlayerSpin(Campaign campaign, Player player)
    {
        if (!campaign.IsCurrentlyActive())
        {
            _logger.LogDebug("Campaign {CampaignId} is not currently active", campaign.Id);
            return false;
        }

        if (!player.CanAffordSpin(campaign.TokenCostPerSpin))
        {
            _logger.LogDebug("Player {PlayerId} cannot afford spin cost {Cost}", 
                player.Id, campaign.TokenCostPerSpin);
            return false;
        }

        var today = DateTime.UtcNow.Date;
        var todaySpins = campaign.WheelSpins.Count(s => s.PlayerId == player.Id && s.CreatedAt.Date == today);
        
        if (todaySpins >= campaign.MaxSpinsPerDay)
        {
            _logger.LogDebug("Player {PlayerId} has reached daily spin limit {Limit} for campaign {CampaignId}", 
                player.Id, campaign.MaxSpinsPerDay, campaign.Id);
            return false;
        }

        return true;
    }

    public int CalculateTokensToAward(Campaign campaign)
    {
        // Base token award - could be configurable per campaign in the future
        return 1;
    }

    public bool ValidateSpinResult(WheelSpin spin, Campaign campaign)
    {
        if (spin.TokensSpent != campaign.TokenCostPerSpin)
        {
            _logger.LogWarning("Invalid token cost in spin {SpinId}: expected {Expected}, got {Actual}", 
                spin.Id, campaign.TokenCostPerSpin, spin.TokensSpent);
            return false;
        }

        if (string.IsNullOrEmpty(spin.RandomSeed))
        {
            _logger.LogWarning("Missing random seed in spin {SpinId}", spin.Id);
            return false;
        }

        if (spin.PrizeId.HasValue)
        {
            var prize = campaign.Prizes.FirstOrDefault(p => p.Id == spin.PrizeId.Value);
            if (prize == null || !prize.IsAvailable())
            {
                _logger.LogWarning("Invalid or unavailable prize {PrizeId} in spin {SpinId}", 
                    spin.PrizeId.Value, spin.Id);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Generates a cryptographically secure random double between 0.0 and 1.0
    /// </summary>
    private double GenerateSecureRandomDouble()
    {
        var bytes = new byte[8];
        _rng.GetBytes(bytes);
        
        // Convert to ulong and normalize to [0, 1)
        var randomULong = BitConverter.ToUInt64(bytes, 0);
        return (double)randomULong / ulong.MaxValue;
    }

    /// <summary>
    /// Selects a prize based on configured probabilities and available inventory
    /// Uses the OddsManagementService for sophisticated probability calculations
    /// </summary>
    private async Task<Prize?> SelectPrizeByProbabilityAsync(IList<Prize> availablePrizes, double randomValue, Guid campaignId)
    {
        if (!availablePrizes.Any())
            return null;

        // Get effective odds from the odds management service
        var effectiveOdds = await _oddsManagement.GetEffectiveOddsAsync(campaignId);
        
        // Use weighted random selection with effective odds
        double cumulativeProbability = 0.0;
        
        foreach (var prize in availablePrizes)
        {
            if (effectiveOdds.TryGetValue(prize.Id, out var prizeOdds))
            {
                cumulativeProbability += (double)prizeOdds;
                
                if (randomValue <= cumulativeProbability)
                {
                    _logger.LogDebug("Selected prize {PrizeId} with effective odds {Odds} (random: {Random})", 
                        prize.Id, prizeOdds, randomValue);
                    return prize;
                }
            }
        }

        // Fallback - should not happen with proper probability normalization
        _logger.LogWarning("No prize selected with random value {Random}, returning null", randomValue);
        return null;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _rng?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}