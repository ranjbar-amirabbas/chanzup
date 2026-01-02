using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chanzup.Application.Services;

public class TokenTransactionService : ITokenTransactionService
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenLimitService _tokenLimitService;

    public TokenTransactionService(IApplicationDbContext context, ITokenLimitService tokenLimitService)
    {
        _context = context;
        _tokenLimitService = tokenLimitService;
    }

    public async Task<TokenTransaction> EarnTokensAsync(Guid playerId, int amount, string description, Guid? relatedEntityId = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        // Validate limits
        var validation = await _tokenLimitService.ValidateTokenEarningAsync(playerId, amount);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.ErrorMessage);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null)
                throw new InvalidOperationException($"Player with ID {playerId} not found");

            if (!player.IsActive)
                throw new InvalidOperationException("Player account is not active");

            // Create transaction record
            var tokenTransaction = TokenTransaction.CreateEarned(playerId, amount, description, relatedEntityId);
            _context.TokenTransactions.Add(tokenTransaction);

            // Update player balance
            player.AddTokens(amount, description);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return tokenTransaction;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<TokenTransaction> SpendTokensAsync(Guid playerId, int amount, string description, Guid? relatedEntityId = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        // Validate limits
        var validation = await _tokenLimitService.ValidateTokenSpendingAsync(playerId, amount);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.ErrorMessage);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null)
                throw new InvalidOperationException($"Player with ID {playerId} not found");

            if (!player.IsActive)
                throw new InvalidOperationException("Player account is not active");

            if (!player.CanAffordSpin(amount))
                throw new InvalidOperationException("Insufficient token balance");

            // Create transaction record
            var tokenTransaction = TokenTransaction.CreateSpent(playerId, amount, description, relatedEntityId);
            _context.TokenTransactions.Add(tokenTransaction);

            // Update player balance
            player.SpendTokens(amount, description);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return tokenTransaction;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<TokenTransaction> PurchaseTokensAsync(Guid playerId, int amount, decimal cost, string paymentReference)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        if (cost <= 0)
            throw new ArgumentException("Cost must be positive", nameof(cost));

        if (string.IsNullOrWhiteSpace(paymentReference))
            throw new ArgumentException("Payment reference is required", nameof(paymentReference));

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null)
                throw new InvalidOperationException($"Player with ID {playerId} not found");

            if (!player.IsActive)
                throw new InvalidOperationException("Player account is not active");

            // Create transaction record
            var tokenTransaction = new TokenTransaction
            {
                PlayerId = playerId,
                Type = TransactionType.Purchased,
                Amount = amount,
                Description = $"Purchased {amount} tokens for ${cost:F2} (Ref: {paymentReference})",
                RelatedEntityId = null
            };
            _context.TokenTransactions.Add(tokenTransaction);

            // Update player balance
            player.AddTokens(amount, tokenTransaction.Description);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return tokenTransaction;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<TokenTransaction> AwardBonusTokensAsync(Guid playerId, int amount, string description, Guid? relatedEntityId = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null)
                throw new InvalidOperationException($"Player with ID {playerId} not found");

            if (!player.IsActive)
                throw new InvalidOperationException("Player account is not active");

            // Create transaction record
            var tokenTransaction = new TokenTransaction
            {
                PlayerId = playerId,
                Type = TransactionType.Bonus,
                Amount = amount,
                Description = description,
                RelatedEntityId = relatedEntityId
            };
            _context.TokenTransactions.Add(tokenTransaction);

            // Update player balance
            player.AddTokens(amount, description);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return tokenTransaction;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<TokenTransaction>> GetTransactionHistoryAsync(Guid playerId, int skip = 0, int take = 50)
    {
        if (take > 100)
            take = 100; // Limit maximum page size

        return await _context.TokenTransactions
            .Where(t => t.PlayerId == playerId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> GetTokenBalanceAsync(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId);
        return player?.TokenBalance ?? 0;
    }

    public async Task<bool> HasSufficientTokensAsync(Guid playerId, int amount)
    {
        var player = await _context.Players.FindAsync(playerId);
        return player?.CanAffordSpin(amount) ?? false;
    }

    public async Task<int> GetDailyTokenEarningsAsync(Guid playerId, DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await _context.TokenTransactions
            .Where(t => t.PlayerId == playerId &&
                       (t.Type == TransactionType.Earned || t.Type == TransactionType.Bonus) &&
                       t.CreatedAt >= startOfDay &&
                       t.CreatedAt < endOfDay)
            .SumAsync(t => t.Amount);
    }

    public async Task<int> GetWeeklyTokenEarningsAsync(Guid playerId, DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(7);

        return await _context.TokenTransactions
            .Where(t => t.PlayerId == playerId &&
                       (t.Type == TransactionType.Earned || t.Type == TransactionType.Bonus) &&
                       t.CreatedAt >= weekStart &&
                       t.CreatedAt < weekEnd)
            .SumAsync(t => t.Amount);
    }

    public async Task<int> GetDailyTokenEarningsAtBusinessAsync(Guid playerId, Guid businessId, DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        // Get QR sessions for the business on the specified date
        var qrSessionIds = await _context.QRSessions
            .Where(q => q.PlayerId == playerId &&
                       q.BusinessId == businessId &&
                       q.CreatedAt >= startOfDay &&
                       q.CreatedAt < endOfDay)
            .Select(q => q.Id)
            .ToListAsync();

        // Sum tokens earned from those QR sessions
        return await _context.TokenTransactions
            .Where(t => t.PlayerId == playerId &&
                       (t.Type == TransactionType.Earned || t.Type == TransactionType.Bonus) &&
                       t.RelatedEntityId.HasValue &&
                       qrSessionIds.Contains(t.RelatedEntityId.Value) &&
                       t.CreatedAt >= startOfDay &&
                       t.CreatedAt < endOfDay)
            .SumAsync(t => t.Amount);
    }

    public async Task<bool> ProcessAtomicTransactionAsync(Guid playerId, int amount, string description, 
        TransactionType type, Guid? relatedEntityId = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null)
                return false;

            if (!player.IsActive)
                return false;

            // Validate transaction based on type
            if (type == TransactionType.Spent && !player.CanAffordSpin(amount))
                return false;

            // Create transaction record
            var tokenTransaction = new TokenTransaction
            {
                PlayerId = playerId,
                Type = type,
                Amount = amount,
                Description = description,
                RelatedEntityId = relatedEntityId
            };
            _context.TokenTransactions.Add(tokenTransaction);

            // Update player balance
            if (type == TransactionType.Spent)
            {
                player.SpendTokens(amount, description);
            }
            else
            {
                player.AddTokens(amount, description);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }
}