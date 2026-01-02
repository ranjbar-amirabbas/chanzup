using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chanzup.Application.Services;

public class TokenLimitService : ITokenLimitService
{
    private readonly IApplicationDbContext _context;

    public TokenLimitService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanEarnTokensAsync(Guid playerId, int amount, Guid? businessId = null)
    {
        var validation = await ValidateTokenEarningAsync(playerId, amount, businessId);
        return validation.IsValid;
    }

    public async Task<bool> CanSpendTokensAsync(Guid playerId, int amount)
    {
        var validation = await ValidateTokenSpendingAsync(playerId, amount);
        return validation.IsValid;
    }

    public async Task<int> GetRemainingDailyEarningCapacityAsync(Guid playerId, Guid? businessId = null)
    {
        var limits = await GetPlayerLimitsAsync(playerId);
        var today = DateTime.UtcNow.Date;

        if (businessId.HasValue)
        {
            var dailyEarningsAtBusiness = await GetDailyTokenEarningsAtBusinessAsync(playerId, businessId.Value, today);
            return Math.Max(0, limits.DailyEarningLimitPerBusiness - dailyEarningsAtBusiness);
        }
        else
        {
            var dailyEarnings = await GetDailyTokenEarningsAsync(playerId, today);
            return Math.Max(0, limits.DailyEarningLimit - dailyEarnings);
        }
    }

    public async Task<int> GetRemainingWeeklyEarningCapacityAsync(Guid playerId)
    {
        var limits = await GetPlayerLimitsAsync(playerId);
        var weekStart = GetStartOfWeek(DateTime.UtcNow);
        var weeklyEarnings = await GetWeeklyTokenEarningsAsync(playerId, weekStart);
        
        return Math.Max(0, limits.WeeklyEarningLimit - weeklyEarnings);
    }

    public async Task<int> GetRemainingDailySpendingCapacityAsync(Guid playerId)
    {
        var limits = await GetPlayerLimitsAsync(playerId);
        var today = DateTime.UtcNow.Date;
        var endOfDay = today.AddDays(1);

        var dailySpending = await _context.TokenTransactions
            .Where(t => t.PlayerId == playerId &&
                       t.Type == TransactionType.Spent &&
                       t.CreatedAt >= today &&
                       t.CreatedAt < endOfDay)
            .SumAsync(t => t.Amount);

        return Math.Max(0, limits.DailySpendingLimit - dailySpending);
    }

    public async Task<TokenLimitValidationResult> ValidateTokenEarningAsync(Guid playerId, int amount, Guid? businessId = null)
    {
        if (amount <= 0)
        {
            return TokenLimitValidationResult.Failure("Amount must be positive", 0, amount, LimitType.DailyEarning);
        }

        var limits = await GetPlayerLimitsAsync(playerId);
        var today = DateTime.UtcNow.Date;

        // Check daily earning limit
        var dailyEarnings = await GetDailyTokenEarningsAsync(playerId, today);
        var remainingDailyCapacity = limits.DailyEarningLimit - dailyEarnings;
        
        if (amount > remainingDailyCapacity)
        {
            return TokenLimitValidationResult.Failure(
                $"Daily earning limit exceeded. Can earn {remainingDailyCapacity} more tokens today.",
                remainingDailyCapacity, amount, LimitType.DailyEarning);
        }

        // Check weekly earning limit
        var weekStart = GetStartOfWeek(DateTime.UtcNow);
        var weeklyEarnings = await GetWeeklyTokenEarningsAsync(playerId, weekStart);
        var remainingWeeklyCapacity = limits.WeeklyEarningLimit - weeklyEarnings;
        
        if (amount > remainingWeeklyCapacity)
        {
            return TokenLimitValidationResult.Failure(
                $"Weekly earning limit exceeded. Can earn {remainingWeeklyCapacity} more tokens this week.",
                remainingWeeklyCapacity, amount, LimitType.WeeklyEarning);
        }

        // Check business-specific daily limit if applicable
        if (businessId.HasValue)
        {
            var dailyEarningsAtBusiness = await GetDailyTokenEarningsAtBusinessAsync(playerId, businessId.Value, today);
            var remainingBusinessCapacity = limits.DailyEarningLimitPerBusiness - dailyEarningsAtBusiness;
            
            if (amount > remainingBusinessCapacity)
            {
                return TokenLimitValidationResult.Failure(
                    $"Daily earning limit per business exceeded. Can earn {remainingBusinessCapacity} more tokens at this business today.",
                    remainingBusinessCapacity, amount, LimitType.DailyEarningPerBusiness);
            }
        }

        // Check maximum balance limit
        var currentBalance = await GetTokenBalanceAsync(playerId);
        var remainingBalanceCapacity = limits.MaxTokenBalance - currentBalance;
        
        if (amount > remainingBalanceCapacity)
        {
            return TokenLimitValidationResult.Failure(
                $"Maximum token balance limit exceeded. Can hold {remainingBalanceCapacity} more tokens.",
                remainingBalanceCapacity, amount, LimitType.MaxBalance);
        }

        return TokenLimitValidationResult.Success(Math.Min(remainingDailyCapacity, remainingWeeklyCapacity), amount);
    }

    public async Task<TokenLimitValidationResult> ValidateTokenSpendingAsync(Guid playerId, int amount)
    {
        if (amount <= 0)
        {
            return TokenLimitValidationResult.Failure("Amount must be positive", 0, amount, LimitType.DailySpending);
        }

        var limits = await GetPlayerLimitsAsync(playerId);
        var today = DateTime.UtcNow.Date;
        var endOfDay = today.AddDays(1);

        // Check daily spending limit
        var dailySpending = await _context.TokenTransactions
            .Where(t => t.PlayerId == playerId &&
                       t.Type == TransactionType.Spent &&
                       t.CreatedAt >= today &&
                       t.CreatedAt < endOfDay)
            .SumAsync(t => t.Amount);

        var remainingSpendingCapacity = limits.DailySpendingLimit - dailySpending;
        
        if (amount > remainingSpendingCapacity)
        {
            return TokenLimitValidationResult.Failure(
                $"Daily spending limit exceeded. Can spend {remainingSpendingCapacity} more tokens today.",
                remainingSpendingCapacity, amount, LimitType.DailySpending);
        }

        // Check if player has sufficient balance
        var currentBalance = await GetTokenBalanceAsync(playerId);
        if (currentBalance < amount)
        {
            return TokenLimitValidationResult.Failure(
                $"Insufficient token balance. Current balance: {currentBalance}, required: {amount}",
                currentBalance, amount, LimitType.DailySpending);
        }

        return TokenLimitValidationResult.Success(remainingSpendingCapacity, amount);
    }

    public async Task<PlayerTokenLimits> GetPlayerLimitsAsync(Guid playerId)
    {
        // For now, return default limits. In the future, this could be customized per player
        // based on subscription tier, player level, or other factors
        var player = await _context.Players.FindAsync(playerId);
        if (player == null)
        {
            throw new InvalidOperationException($"Player with ID {playerId} not found");
        }

        // Default limits - could be enhanced to support different tiers
        return new PlayerTokenLimits
        {
            DailyEarningLimit = 100,
            WeeklyEarningLimit = 500,
            DailySpendingLimit = 200,
            DailyEarningLimitPerBusiness = 50,
            MaxTokenBalance = 1000
        };
    }

    private async Task<int> GetDailyTokenEarningsAsync(Guid playerId, DateTime date)
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

    private async Task<int> GetWeeklyTokenEarningsAsync(Guid playerId, DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(7);

        return await _context.TokenTransactions
            .Where(t => t.PlayerId == playerId &&
                       (t.Type == TransactionType.Earned || t.Type == TransactionType.Bonus) &&
                       t.CreatedAt >= weekStart &&
                       t.CreatedAt < weekEnd)
            .SumAsync(t => t.Amount);
    }

    private async Task<int> GetDailyTokenEarningsAtBusinessAsync(Guid playerId, Guid businessId, DateTime date)
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

    private async Task<int> GetTokenBalanceAsync(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId);
        return player?.TokenBalance ?? 0;
    }

    private static DateTime GetStartOfWeek(DateTime date)
    {
        // Get Monday as start of week
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }
}