using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;
using Chanzup.Domain.Services;
using Chanzup.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chanzup.Application.Services;

public class WheelSpinService : IWheelSpinService
{
    private readonly IApplicationDbContext _context;
    private readonly IGameEngineService _gameEngine;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<WheelSpinService> _logger;

    public WheelSpinService(
        IApplicationDbContext context,
        IGameEngineService gameEngine,
        IAnalyticsService analyticsService,
        ILogger<WheelSpinService> logger)
    {
        _context = context;
        _gameEngine = gameEngine;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public async Task<WheelSpinResult> ProcessSpinAsync(Guid playerId, Guid campaignId, string sessionId)
    {
        // Note: Transaction handling would need to be implemented at a higher level
        // since IApplicationDbContext doesn't expose Database property
        
        try
        {
            // Load player and campaign with related data
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Id == playerId);
                
            if (player == null)
            {
                return new WheelSpinResult
                {
                    Success = false,
                    ErrorMessage = "Player not found"
                };
            }

            var campaign = await _context.Campaigns
                .Include(c => c.Prizes)
                .Include(c => c.WheelSpins.Where(ws => ws.PlayerId == playerId))
                .FirstOrDefaultAsync(c => c.Id == campaignId);
                
            if (campaign == null)
            {
                return new WheelSpinResult
                {
                    Success = false,
                    ErrorMessage = "Campaign not found"
                };
            }

            // Validate spin eligibility
            if (!_gameEngine.CanPlayerSpin(campaign, player))
            {
                return new WheelSpinResult
                {
                    Success = false,
                    ErrorMessage = "Player cannot spin wheel at this time"
                };
            }

            // Generate random seed for audit trail
            var randomSeed = WheelSpin.GenerateRandomSeed();
            
            // Perform the spin
            var prizeWon = await _gameEngine.SpinWheel(campaign, player);
            
            // Deduct tokens atomically
            player.SpendTokens(campaign.TokenCostPerSpin, $"Wheel spin for campaign {campaign.Name}");
            
            // Create wheel spin record
            var wheelSpin = new WheelSpin
            {
                PlayerId = playerId,
                CampaignId = campaignId,
                PrizeId = prizeWon?.Id,
                TokensSpent = campaign.TokenCostPerSpin,
                SpinResult = prizeWon?.Name ?? "No Prize",
                RandomSeed = randomSeed,
                CreatedAt = DateTime.UtcNow
            };

            _context.WheelSpins.Add(wheelSpin);

            // If prize was won, update inventory and create player prize
            if (prizeWon != null)
            {
                // Reserve the prize (decrements inventory)
                prizeWon.ReserveOne();
                
                // Create player prize record
                var playerPrize = new PlayerPrize
                {
                    PlayerId = playerId,
                    PrizeId = prizeWon.Id,
                    RedemptionCode = RedemptionCode.Generate(),
                    ExpiresAt = DateTime.UtcNow.AddDays(30), // 30-day expiration
                    IsRedeemed = false
                };

                _context.PlayerPrizes.Add(playerPrize);
                
                _logger.LogInformation("Player {PlayerId} won prize {PrizeId} in campaign {CampaignId}", 
                    playerId, prizeWon.Id, campaignId);
            }

            // Create token transaction record
            var tokenTransaction = new TokenTransaction
            {
                PlayerId = playerId,
                Amount = campaign.TokenCostPerSpin,
                Type = TransactionType.Spent,
                Description = $"Wheel spin - {campaign.Name}",
                RelatedEntityId = wheelSpin.Id
            };

            _context.TokenTransactions.Add(tokenTransaction);

            // Save all changes
            await _context.SaveChangesAsync();

            // Track analytics event
            await _analyticsService.TrackWheelSpinAsync(
                campaign.BusinessId, 
                playerId, 
                campaignId, 
                campaign.TokenCostPerSpin, 
                prizeWon != null, 
                prizeWon?.Id);

            _logger.LogInformation("Successful wheel spin for player {PlayerId} in campaign {CampaignId}: {Result}", 
                playerId, campaignId, wheelSpin.SpinResult);

            return new WheelSpinResult
            {
                SpinId = wheelSpin.Id,
                Success = true,
                PrizeWon = prizeWon,
                TokensSpent = campaign.TokenCostPerSpin,
                NewTokenBalance = player.TokenBalance,
                RandomSeed = randomSeed,
                SpinTime = wheelSpin.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing wheel spin for player {PlayerId} in campaign {CampaignId}", 
                playerId, campaignId);
                
            return new WheelSpinResult
            {
                Success = false,
                ErrorMessage = "An error occurred while processing the spin"
            };
        }
    }

    public async Task<bool> CanPlayerSpinAsync(Guid playerId, Guid campaignId)
    {
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.Id == playerId);
            
        if (player == null)
            return false;

        var campaign = await _context.Campaigns
            .Include(c => c.WheelSpins.Where(ws => ws.PlayerId == playerId))
            .FirstOrDefaultAsync(c => c.Id == campaignId);
            
        if (campaign == null)
            return false;

        return _gameEngine.CanPlayerSpin(campaign, player);
    }

    public async Task<int> GetRemainingSpinsAsync(Guid playerId, Guid campaignId)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.WheelSpins.Where(ws => ws.PlayerId == playerId))
            .FirstOrDefaultAsync(c => c.Id == campaignId);
            
        if (campaign == null)
            return 0;

        var today = DateTime.UtcNow.Date;
        var todaySpins = campaign.WheelSpins.Count(s => s.PlayerId == playerId && s.CreatedAt.Date == today);
        
        return Math.Max(0, campaign.MaxSpinsPerDay - todaySpins);
    }
}