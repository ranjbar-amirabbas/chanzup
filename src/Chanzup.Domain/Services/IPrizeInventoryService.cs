using Chanzup.Domain.Entities;

namespace Chanzup.Domain.Services;

public interface IPrizeInventoryService
{
    Task<bool> ReservePrize(Prize prize);
    Task ReleasePrize(Prize prize);
    Task<bool> IsPrizeAvailable(Prize prize);
    Task UpdateInventoryAfterSpin(Prize prize);
    Task<IEnumerable<Prize>> GetAvailablePrizes(Campaign campaign);
}