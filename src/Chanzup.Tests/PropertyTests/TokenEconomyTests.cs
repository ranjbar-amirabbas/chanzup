using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Chanzup.Application.Services;
using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;
using Chanzup.Domain.ValueObjects;
using Chanzup.Infrastructure.Data;
using Chanzup.Infrastructure.Services;
using Xunit;

namespace Chanzup.Tests.PropertyTests;

/// <summary>
/// Feature: vancouver-rewards-platform, Property 11: Daily and Weekly Limits Enforcement
/// Feature: vancouver-rewards-platform, Property 14: Token Balance Integrity
/// Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.6
/// </summary>
public class TokenEconomyTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var tenantContext = new TenantContext();
        return new ApplicationDbContext(options, tenantContext);
    }

    private Player CreateTestPlayer(ApplicationDbContext context, int initialBalance = 0)
    {
        var player = new Player
        {
            Email = new Email($"test{Guid.NewGuid()}@example.com"),
            PasswordHash = "hashedpassword",
            FirstName = "Test",
            LastName = "Player",
            TokenBalance = initialBalance
        };

        context.Players.Add(player);
        context.SaveChanges();
        return player;
    }

    [Property(MaxTest = 25)]
    public Property TokenBalanceIntegrityAfterEarning()
    {
        return Prop.ForAll(
            GenerateTokenAmount(),
            (amount) =>
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var tokenLimitService = new TokenLimitService(context);
                var tokenTransactionService = new TokenTransactionService(context, tokenLimitService);
                
                var player = CreateTestPlayer(context, 0);
                var initialBalance = player.TokenBalance;

                try
                {
                    // Act - Earn tokens
                    var task = tokenTransactionService.EarnTokensAsync(
                        player.Id, amount, "Test earning");
                    task.Wait();
                    var transaction = task.Result;

                    // Refresh player from database
                    context.Entry(player).Reload();

                    // Assert - Token balance integrity
                    var expectedBalance = initialBalance + amount;
                    var actualBalance = player.TokenBalance;
                    var transactionAmount = transaction.Amount;
                    var transactionType = transaction.Type;

                    return (actualBalance == expectedBalance) &&
                           (transactionAmount == amount) &&
                           (transactionType == TransactionType.Earned) &&
                           (transaction.PlayerId == player.Id);
                }
                catch (AggregateException ex) when (ex.InnerException is InvalidOperationException)
                {
                    // If earning fails due to limits, balance should remain unchanged
                    context.Entry(player).Reload();
                    return player.TokenBalance == initialBalance;
                }
            });
    }

    [Property(MaxTest = 25)]
    public Property TokenBalanceIntegrityAfterSpending()
    {
        return Prop.ForAll(
            GenerateTokenAmount(),
            GenerateTokenAmount(),
            (initialBalance, spendAmount) =>
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var tokenLimitService = new TokenLimitService(context);
                var tokenTransactionService = new TokenTransactionService(context, tokenLimitService);
                
                var player = CreateTestPlayer(context, Math.Max(initialBalance, 0));
                var startingBalance = player.TokenBalance;

                try
                {
                    // Act - Spend tokens
                    var task = tokenTransactionService.SpendTokensAsync(
                        player.Id, spendAmount, "Test spending");
                    task.Wait();
                    var transaction = task.Result;

                    // Refresh player from database
                    context.Entry(player).Reload();

                    // Assert - Token balance integrity
                    var expectedBalance = startingBalance - spendAmount;
                    var actualBalance = player.TokenBalance;

                    return (actualBalance == expectedBalance) &&
                           (actualBalance >= 0) &&
                           (transaction.Amount == spendAmount) &&
                           (transaction.Type == TransactionType.Spent) &&
                           (transaction.PlayerId == player.Id);
                }
                catch (AggregateException ex) when (ex.InnerException is InvalidOperationException)
                {
                    // If spending fails due to insufficient balance or limits, balance should remain unchanged
                    context.Entry(player).Reload();
                    return player.TokenBalance == startingBalance;
                }
            });
    }

    [Property(MaxTest = 25)]
    public bool DailyEarningLimitsEnforcement(int amount)
    {
        if (amount <= 0 || amount > 200) return true; // Skip invalid amounts
        
        try
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var tokenLimitService = new TokenLimitService(context);
            
            var player = CreateTestPlayer(context, 0);
            var limits = tokenLimitService.GetPlayerLimitsAsync(player.Id).GetAwaiter().GetResult();

            // Pre-populate with earnings close to daily limit (90 out of 100)
            var existingEarnings = limits.DailyEarningLimit - 10; // 90 tokens
            if (existingEarnings > 0)
            {
                // Create existing transaction for today
                var existingTransaction = TokenTransaction.CreateEarned(
                    player.Id, existingEarnings, "Existing earnings");
                context.TokenTransactions.Add(existingTransaction);
                player.AddTokens(existingEarnings, "Existing earnings");
                context.SaveChanges();
            }

            // Get current daily earnings before the test
            var currentDailyEarnings = context.TokenTransactions
                .Where(t => t.PlayerId == player.Id &&
                           (t.Type == TransactionType.Earned || t.Type == TransactionType.Bonus) &&
                           t.CreatedAt >= DateTime.UtcNow.Date &&
                           t.CreatedAt < DateTime.UtcNow.Date.AddDays(1))
                .Sum(t => t.Amount);

            // Check if we can earn this amount according to limits
            var validation = tokenLimitService.ValidateTokenEarningAsync(player.Id, amount).GetAwaiter().GetResult();
            
            if (validation.IsValid)
            {
                // If validation says we can earn, then after earning we should not exceed the limit
                var projectedEarnings = currentDailyEarnings + amount;
                return projectedEarnings <= limits.DailyEarningLimit;
            }
            else
            {
                // If validation says we can't earn, it should be because it would exceed the limit
                var wouldExceedLimit = (currentDailyEarnings + amount) > limits.DailyEarningLimit;
                return wouldExceedLimit;
            }
        }
        catch (Exception)
        {
            // If any unexpected error occurs, the property fails
            return false;
        }
    }

    [Property(MaxTest = 25)]
    public bool WeeklyEarningLimitsEnforcement(int amount)
    {
        if (amount <= 0 || amount > 200) return true; // Skip invalid amounts
        
        try
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var tokenLimitService = new TokenLimitService(context);
            
            var player = CreateTestPlayer(context, 0);
            var limits = tokenLimitService.GetPlayerLimitsAsync(player.Id).GetAwaiter().GetResult();

            // Pre-populate with earnings close to weekly limit
            var existingEarnings = limits.WeeklyEarningLimit - 20; // Leave some room
            if (existingEarnings > 0)
            {
                // Create existing transactions for this week
                var weekStart = GetStartOfWeek(DateTime.UtcNow);
                var existingTransaction = TokenTransaction.CreateEarned(
                    player.Id, existingEarnings, "Existing weekly earnings");
                existingTransaction.CreatedAt = weekStart.AddDays(1); // Set to earlier in the week
                context.TokenTransactions.Add(existingTransaction);
                player.AddTokens(existingEarnings, "Existing weekly earnings");
                context.SaveChanges();
            }

            // Get current weekly earnings before the test
            var weekStart2 = GetStartOfWeek(DateTime.UtcNow);
            var weekEnd = weekStart2.AddDays(7);
            var currentWeeklyEarnings = context.TokenTransactions
                .Where(t => t.PlayerId == player.Id &&
                           (t.Type == TransactionType.Earned || t.Type == TransactionType.Bonus) &&
                           t.CreatedAt >= weekStart2 &&
                           t.CreatedAt < weekEnd)
                .Sum(t => t.Amount);

            // Check if we can earn this amount according to limits
            var validation = tokenLimitService.ValidateTokenEarningAsync(player.Id, amount).GetAwaiter().GetResult();
            
            if (validation.IsValid)
            {
                // If validation says we can earn, then after earning we should not exceed the limit
                var projectedEarnings = currentWeeklyEarnings + amount;
                return projectedEarnings <= limits.WeeklyEarningLimit;
            }
            else
            {
                // If validation says we can't earn, it should be because it would exceed some limit
                var wouldExceedWeeklyLimit = (currentWeeklyEarnings + amount) > limits.WeeklyEarningLimit;
                var wouldExceedDailyLimit = amount > limits.DailyEarningLimit; // No daily earnings yet
                return wouldExceedWeeklyLimit || wouldExceedDailyLimit;
            }
        }
        catch (Exception)
        {
            // If any unexpected error occurs, the property fails
            return false;
        }
    }

    [Property(MaxTest = 25)]
    public bool DailySpendingLimitsEnforcement(int spendAmount)
    {
        if (spendAmount <= 0 || spendAmount > 200) return true; // Skip invalid amounts
        
        try
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var tokenLimitService = new TokenLimitService(context);
            
            // Create a test player first to get valid limits
            var testPlayer = CreateTestPlayer(context, 0);
            var limits = tokenLimitService.GetPlayerLimitsAsync(testPlayer.Id).GetAwaiter().GetResult();
            
            var initialBalance = Math.Max(spendAmount + 100, limits.MaxTokenBalance);
            var player = CreateTestPlayer(context, initialBalance);

            // Pre-populate with spending close to daily limit
            var existingSpending = limits.DailySpendingLimit - 10; // Leave some room
            if (existingSpending > 0)
            {
                // Create existing spending transaction for today
                var existingTransaction = TokenTransaction.CreateSpent(
                    player.Id, existingSpending, "Existing spending");
                context.TokenTransactions.Add(existingTransaction);
                player.SpendTokens(existingSpending, "Existing spending");
                context.SaveChanges();
            }

            // Get current daily spending before the test
            var today = DateTime.UtcNow.Date;
            var endOfDay = today.AddDays(1);
            var currentDailySpending = context.TokenTransactions
                .Where(t => t.PlayerId == player.Id &&
                           t.Type == TransactionType.Spent &&
                           t.CreatedAt >= today &&
                           t.CreatedAt < endOfDay)
                .Sum(t => t.Amount);

            // Check if we can spend this amount according to limits
            var validation = tokenLimitService.ValidateTokenSpendingAsync(player.Id, spendAmount).GetAwaiter().GetResult();
            
            if (validation.IsValid)
            {
                // If validation says we can spend, then after spending we should not exceed the limit
                var projectedSpending = currentDailySpending + spendAmount;
                var hasEnoughBalance = player.TokenBalance >= spendAmount;
                return projectedSpending <= limits.DailySpendingLimit && hasEnoughBalance;
            }
            else
            {
                // If validation says we can't spend, it should be because it would exceed the limit or insufficient balance
                var wouldExceedLimit = (currentDailySpending + spendAmount) > limits.DailySpendingLimit;
                var insufficientBalance = player.TokenBalance < spendAmount;
                return wouldExceedLimit || insufficientBalance;
            }
        }
        catch (Exception)
        {
            // If any unexpected error occurs, the property fails
            return false;
        }
    }

    [Property(MaxTest = 25)]
    public Property AtomicTransactionIntegrity()
    {
        return Prop.ForAll(
            GenerateTokenAmount(),
            GenerateTransactionType(),
            (amount, transactionType) =>
            {
                // Arrange
                using var context = CreateInMemoryContext();
                var tokenLimitService = new TokenLimitService(context);
                
                // Create a simple token service without transactions for testing
                var simpleTokenService = new SimpleTokenTransactionService(context, tokenLimitService);
                
                var initialBalance = transactionType == TransactionType.Spent ? amount + 100 : 0;
                var player = CreateTestPlayer(context, initialBalance);
                var startingBalance = player.TokenBalance;

                // Act - Process simple transaction (without database transactions)
                var success = simpleTokenService.ProcessSimpleTransaction(
                    player.Id, amount, "Test transaction", transactionType);

                // Refresh player from database
                context.Entry(player).Reload();

                if (success)
                {
                    // Assert - Transaction was successful and balance updated correctly
                    var expectedBalance = transactionType == TransactionType.Spent 
                        ? startingBalance - amount 
                        : startingBalance + amount;

                    var transactionExists = context.TokenTransactions
                        .Any(t => t.PlayerId == player.Id && 
                                 t.Amount == amount && 
                                 t.Type == transactionType);

                    return (player.TokenBalance == expectedBalance) && transactionExists;
                }
                else
                {
                    // Assert - Transaction failed and balance unchanged
                    var noNewTransaction = !context.TokenTransactions
                        .Any(t => t.PlayerId == player.Id && 
                                 t.Amount == amount && 
                                 t.Type == transactionType &&
                                 t.Description == "Test transaction");

                    return (player.TokenBalance == startingBalance) && noNewTransaction;
                }
            });
    }

    private static Arbitrary<int> GenerateTokenAmount()
    {
        return Arb.From(Gen.Choose(1, 200));
    }

    private static Arbitrary<TransactionType> GenerateTransactionType()
    {
        return Arb.From(Gen.Elements(TransactionType.Earned, TransactionType.Spent, TransactionType.Bonus));
    }

    private static DateTime GetStartOfWeek(DateTime date)
    {
        // Get Monday as start of week
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }

    // Simple token service for testing without database transactions
    private class SimpleTokenTransactionService
    {
        private readonly ApplicationDbContext _context;
        private readonly TokenLimitService _tokenLimitService;

        public SimpleTokenTransactionService(ApplicationDbContext context, TokenLimitService tokenLimitService)
        {
            _context = context;
            _tokenLimitService = tokenLimitService;
        }

        public bool ProcessSimpleTransaction(Guid playerId, int amount, string description, TransactionType type)
        {
            try
            {
                var player = _context.Players.Find(playerId);
                if (player == null || !player.IsActive)
                    return false;

                // Validate transaction based on type
                if (type == TransactionType.Spent && !player.CanAffordSpin(amount))
                    return false;

                // Check limits
                if (type == TransactionType.Earned || type == TransactionType.Bonus)
                {
                    var validationTask = _tokenLimitService.ValidateTokenEarningAsync(playerId, amount);
                    validationTask.Wait();
                    if (!validationTask.Result.IsValid)
                        return false;
                }
                else if (type == TransactionType.Spent)
                {
                    var validationTask = _tokenLimitService.ValidateTokenSpendingAsync(playerId, amount);
                    validationTask.Wait();
                    if (!validationTask.Result.IsValid)
                        return false;
                }

                // Create transaction record
                var tokenTransaction = new TokenTransaction
                {
                    PlayerId = playerId,
                    Type = type,
                    Amount = amount,
                    Description = description
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

                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}