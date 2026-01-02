using Chanzup.Domain.Entities;

namespace Chanzup.Application.Interfaces;

public interface ITokenTransactionService
{
    /// <summary>
    /// Awards tokens to a player and creates a transaction record
    /// </summary>
    Task<TokenTransaction> EarnTokensAsync(Guid playerId, int amount, string description, Guid? relatedEntityId = null);

    /// <summary>
    /// Deducts tokens from a player and creates a transaction record
    /// </summary>
    Task<TokenTransaction> SpendTokensAsync(Guid playerId, int amount, string description, Guid? relatedEntityId = null);

    /// <summary>
    /// Processes token purchase and creates a transaction record
    /// </summary>
    Task<TokenTransaction> PurchaseTokensAsync(Guid playerId, int amount, decimal cost, string paymentReference);

    /// <summary>
    /// Awards bonus tokens (referrals, social sharing, etc.)
    /// </summary>
    Task<TokenTransaction> AwardBonusTokensAsync(Guid playerId, int amount, string description, Guid? relatedEntityId = null);

    /// <summary>
    /// Gets transaction history for a player with pagination
    /// </summary>
    Task<IEnumerable<TokenTransaction>> GetTransactionHistoryAsync(Guid playerId, int skip = 0, int take = 50);

    /// <summary>
    /// Gets player's current token balance
    /// </summary>
    Task<int> GetTokenBalanceAsync(Guid playerId);

    /// <summary>
    /// Validates if player has sufficient tokens for a transaction
    /// </summary>
    Task<bool> HasSufficientTokensAsync(Guid playerId, int amount);

    /// <summary>
    /// Gets daily token earnings for a player
    /// </summary>
    Task<int> GetDailyTokenEarningsAsync(Guid playerId, DateTime date);

    /// <summary>
    /// Gets weekly token earnings for a player
    /// </summary>
    Task<int> GetWeeklyTokenEarningsAsync(Guid playerId, DateTime weekStart);

    /// <summary>
    /// Gets daily token earnings for a player at a specific business
    /// </summary>
    Task<int> GetDailyTokenEarningsAtBusinessAsync(Guid playerId, Guid businessId, DateTime date);

    /// <summary>
    /// Processes atomic token transfer between operations
    /// </summary>
    Task<bool> ProcessAtomicTransactionAsync(Guid playerId, int amount, string description, 
        TransactionType type, Guid? relatedEntityId = null);
}