using Chanzup.Application.DTOs;
using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;
using Chanzup.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chanzup.Application.Services;

public class RedemptionService : IRedemptionService
{
    private readonly IApplicationDbContext _context;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<RedemptionService> _logger;

    public RedemptionService(
        IApplicationDbContext context, 
        IAnalyticsService analyticsService,
        ILogger<RedemptionService> logger)
    {
        _context = context;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public async Task<RedemptionVerificationResponse> VerifyRedemptionCodeAsync(string redemptionCode)
    {
        try
        {
            var playerPrize = await _context.PlayerPrizes
                .Include(pp => pp.Prize)
                    .ThenInclude(p => p.Campaign)
                        .ThenInclude(c => c.Business)
                .Include(pp => pp.Player)
                .FirstOrDefaultAsync(pp => pp.RedemptionCode == redemptionCode);

            if (playerPrize == null)
            {
                return new RedemptionVerificationResponse
                {
                    IsValid = false,
                    CanRedeem = false,
                    ErrorMessage = "Invalid redemption code"
                };
            }

            if (playerPrize.IsRedeemed)
            {
                return new RedemptionVerificationResponse
                {
                    IsValid = true,
                    CanRedeem = false,
                    ErrorMessage = "Prize has already been redeemed",
                    Prize = MapToPlayerPrizeResponse(playerPrize)
                };
            }

            if (playerPrize.IsExpired())
            {
                return new RedemptionVerificationResponse
                {
                    IsValid = true,
                    CanRedeem = false,
                    ErrorMessage = "Prize has expired",
                    Prize = MapToPlayerPrizeResponse(playerPrize)
                };
            }

            return new RedemptionVerificationResponse
            {
                IsValid = true,
                CanRedeem = true,
                Prize = MapToPlayerPrizeResponse(playerPrize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying redemption code {RedemptionCode}", redemptionCode);
            return new RedemptionVerificationResponse
            {
                IsValid = false,
                CanRedeem = false,
                ErrorMessage = "An error occurred while verifying the redemption code"
            };
        }
    }

    public async Task<RedemptionCompletionResponse> CompleteRedemptionAsync(string redemptionCode, Guid staffId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var playerPrize = await _context.PlayerPrizes
                .Include(pp => pp.Prize)
                    .ThenInclude(p => p.Campaign)
                        .ThenInclude(c => c.Business)
                .Include(pp => pp.Player)
                .FirstOrDefaultAsync(pp => pp.RedemptionCode == redemptionCode);

            if (playerPrize == null)
            {
                return new RedemptionCompletionResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid redemption code"
                };
            }

            if (!playerPrize.CanBeRedeemed())
            {
                var errorMessage = playerPrize.IsRedeemed 
                    ? "Prize has already been redeemed" 
                    : "Prize has expired";
                    
                return new RedemptionCompletionResponse
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    Prize = MapToPlayerPrizeResponse(playerPrize)
                };
            }

            // Verify staff has permission to redeem for this business
            var staff = await _context.Staff
                .FirstOrDefaultAsync(s => s.Id == staffId && s.BusinessId == playerPrize.Prize.Campaign.BusinessId);

            if (staff == null)
            {
                return new RedemptionCompletionResponse
                {
                    Success = false,
                    ErrorMessage = "Staff member not authorized to redeem prizes for this business"
                };
            }

            // Complete the redemption
            playerPrize.Redeem();
            
            await _context.SaveChangesAsync();

            // Track analytics event
            await _analyticsService.TrackPrizeRedemptionAsync(
                playerPrize.Prize.Campaign.BusinessId, 
                playerPrize.PlayerId, 
                playerPrize.PrizeId);

            await transaction.CommitAsync();

            _logger.LogInformation("Prize {PrizeId} redeemed by staff {StaffId} for player {PlayerId}", 
                playerPrize.PrizeId, staffId, playerPrize.PlayerId);

            return new RedemptionCompletionResponse
            {
                Success = true,
                RedemptionId = playerPrize.Id,
                RedeemedAt = playerPrize.RedeemedAt,
                Prize = MapToPlayerPrizeResponse(playerPrize)
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error completing redemption for code {RedemptionCode} by staff {StaffId}", 
                redemptionCode, staffId);
            
            return new RedemptionCompletionResponse
            {
                Success = false,
                ErrorMessage = "An error occurred while completing the redemption"
            };
        }
    }

    public async Task<IEnumerable<PlayerPrizeResponse>> GetPlayerPrizesAsync(Guid playerId, bool includeRedeemed = false)
    {
        var query = _context.PlayerPrizes
            .Include(pp => pp.Prize)
                .ThenInclude(p => p.Campaign)
                    .ThenInclude(c => c.Business)
            .Include(pp => pp.Player)
            .Where(pp => pp.PlayerId == playerId);

        if (!includeRedeemed)
        {
            query = query.Where(pp => !pp.IsRedeemed);
        }

        var playerPrizes = await query
            .OrderByDescending(pp => pp.CreatedAt)
            .ToListAsync();

        return playerPrizes.Select(MapToPlayerPrizeResponse);
    }

    public async Task<int> CleanupExpiredPrizesAsync()
    {
        try
        {
            var expiredPrizes = await _context.PlayerPrizes
                .Where(pp => !pp.IsRedeemed && pp.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            if (!expiredPrizes.Any())
            {
                return 0;
            }

            _context.PlayerPrizes.RemoveRange(expiredPrizes);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} expired prizes", expiredPrizes.Count);
            return expiredPrizes.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired prizes");
            return 0;
        }
    }

    public async Task<RedemptionStatsResponse> GetRedemptionStatsAsync(Guid businessId, DateTime? startDate = null, DateTime? endDate = null)
    {
        startDate ??= DateTime.UtcNow.AddDays(-30);
        endDate ??= DateTime.UtcNow;

        var playerPrizes = await _context.PlayerPrizes
            .Include(pp => pp.Prize)
                .ThenInclude(p => p.Campaign)
            .Where(pp => pp.Prize.Campaign.BusinessId == businessId 
                        && pp.CreatedAt >= startDate 
                        && pp.CreatedAt <= endDate)
            .ToListAsync();

        var totalAwarded = playerPrizes.Count;
        var totalRedeemed = playerPrizes.Count(pp => pp.IsRedeemed);
        var totalExpired = playerPrizes.Count(pp => pp.IsExpired() && !pp.IsRedeemed);
        var pendingRedemptions = playerPrizes.Count(pp => pp.CanBeRedeemed());

        var totalValue = playerPrizes.Sum(pp => pp.Prize.Value?.Amount ?? 0);
        var redeemedValue = playerPrizes.Where(pp => pp.IsRedeemed).Sum(pp => pp.Prize.Value?.Amount ?? 0);
        var expiredValue = playerPrizes.Where(pp => pp.IsExpired() && !pp.IsRedeemed).Sum(pp => pp.Prize.Value?.Amount ?? 0);

        var prizeBreakdown = playerPrizes
            .GroupBy(pp => new { pp.Prize.Id, pp.Prize.Name })
            .Select(g => new PrizeRedemptionStat
            {
                PrizeId = g.Key.Id,
                PrizeName = g.Key.Name,
                Awarded = g.Count(),
                Redeemed = g.Count(pp => pp.IsRedeemed),
                Expired = g.Count(pp => pp.IsExpired() && !pp.IsRedeemed),
                RedemptionRate = g.Count() > 0 ? (decimal)g.Count(pp => pp.IsRedeemed) / g.Count() : 0
            })
            .ToList();

        return new RedemptionStatsResponse
        {
            BusinessId = businessId,
            StartDate = startDate.Value,
            EndDate = endDate.Value,
            TotalPrizesAwarded = totalAwarded,
            TotalPrizesRedeemed = totalRedeemed,
            TotalPrizesExpired = totalExpired,
            PendingRedemptions = pendingRedemptions,
            RedemptionRate = totalAwarded > 0 ? (decimal)totalRedeemed / totalAwarded : 0,
            ExpirationRate = totalAwarded > 0 ? (decimal)totalExpired / totalAwarded : 0,
            TotalPrizeValue = totalValue,
            RedeemedPrizeValue = redeemedValue,
            ExpiredPrizeValue = expiredValue,
            PrizeBreakdown = prizeBreakdown
        };
    }

    private static PlayerPrizeResponse MapToPlayerPrizeResponse(PlayerPrize playerPrize)
    {
        return new PlayerPrizeResponse
        {
            Id = playerPrize.Id,
            PlayerId = playerPrize.PlayerId,
            PrizeId = playerPrize.PrizeId,
            RedemptionCode = playerPrize.RedemptionCode,
            IsRedeemed = playerPrize.IsRedeemed,
            RedeemedAt = playerPrize.RedeemedAt,
            ExpiresAt = playerPrize.ExpiresAt,
            CreatedAt = playerPrize.CreatedAt,
            PrizeName = playerPrize.Prize.Name,
            PrizeDescription = playerPrize.Prize.Description,
            PrizeValue = playerPrize.Prize.Value?.Amount,
            BusinessName = playerPrize.Prize.Campaign.Business.Name,
            BusinessAddress = playerPrize.Prize.Campaign.Business.Address,
            PlayerName = $"{playerPrize.Player.FirstName} {playerPrize.Player.LastName}".Trim(),
            PlayerEmail = playerPrize.Player.Email
        };
    }
}