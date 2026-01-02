using Chanzup.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chanzup.Application.Services;

public class PrizeExpirationService : IPrizeExpirationService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<PrizeExpirationService> _logger;

    public PrizeExpirationService(IApplicationDbContext context, ILogger<PrizeExpirationService> logger)
    {
        _context = context;
        _logger = logger;
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

    public async Task<int> CleanupExpiredPrizesForBusinessAsync(Guid businessId)
    {
        try
        {
            var expiredPrizes = await _context.PlayerPrizes
                .Include(pp => pp.Prize)
                    .ThenInclude(p => p.Campaign)
                .Where(pp => !pp.IsRedeemed 
                           && pp.ExpiresAt < DateTime.UtcNow 
                           && pp.Prize.Campaign.BusinessId == businessId)
                .ToListAsync();

            if (!expiredPrizes.Any())
            {
                return 0;
            }

            _context.PlayerPrizes.RemoveRange(expiredPrizes);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} expired prizes for business {BusinessId}", 
                expiredPrizes.Count, businessId);
            return expiredPrizes.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired prizes for business {BusinessId}", businessId);
            return 0;
        }
    }

    public async Task<IEnumerable<Guid>> GetExpiredPrizeIdsAsync(int batchSize = 100)
    {
        try
        {
            return await _context.PlayerPrizes
                .Where(pp => !pp.IsRedeemed && pp.ExpiresAt < DateTime.UtcNow)
                .Take(batchSize)
                .Select(pp => pp.Id)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expired prize IDs");
            return Enumerable.Empty<Guid>();
        }
    }

    public async Task NotifyPlayersOfExpiringPrizesAsync(TimeSpan expirationWarningPeriod)
    {
        try
        {
            var warningThreshold = DateTime.UtcNow.Add(expirationWarningPeriod);
            
            var expiringPrizes = await _context.PlayerPrizes
                .Include(pp => pp.Player)
                .Include(pp => pp.Prize)
                    .ThenInclude(p => p.Campaign)
                        .ThenInclude(c => c.Business)
                .Where(pp => !pp.IsRedeemed 
                           && pp.ExpiresAt > DateTime.UtcNow 
                           && pp.ExpiresAt <= warningThreshold)
                .ToListAsync();

            if (!expiringPrizes.Any())
            {
                _logger.LogDebug("No prizes expiring within {Period}", expirationWarningPeriod);
                return;
            }

            // Group by player to send consolidated notifications
            var playerGroups = expiringPrizes.GroupBy(pp => pp.Player);

            foreach (var playerGroup in playerGroups)
            {
                var player = playerGroup.Key;
                var prizes = playerGroup.ToList();

                // Here you would integrate with your notification service
                // For now, just log the notification
                _logger.LogInformation("Would notify player {PlayerId} ({Email}) about {Count} expiring prizes", 
                    player.Id, player.Email, prizes.Count);

                // TODO: Integrate with email/push notification service
                // await _notificationService.SendExpirationWarningAsync(player, prizes);
            }

            _logger.LogInformation("Processed expiration warnings for {PlayerCount} players with {PrizeCount} expiring prizes", 
                playerGroups.Count(), expiringPrizes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying players of expiring prizes");
        }
    }
}