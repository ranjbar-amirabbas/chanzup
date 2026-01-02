namespace Chanzup.Application.Interfaces;

public interface IPrizeExpirationService
{
    Task<int> CleanupExpiredPrizesAsync();
    Task<int> CleanupExpiredPrizesForBusinessAsync(Guid businessId);
    Task<IEnumerable<Guid>> GetExpiredPrizeIdsAsync(int batchSize = 100);
    Task NotifyPlayersOfExpiringPrizesAsync(TimeSpan expirationWarningPeriod);
}