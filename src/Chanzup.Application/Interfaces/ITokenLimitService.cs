namespace Chanzup.Application.Interfaces;

public interface ITokenLimitService
{
    /// <summary>
    /// Checks if player can earn tokens based on daily limits
    /// </summary>
    Task<bool> CanEarnTokensAsync(Guid playerId, int amount, Guid? businessId = null);

    /// <summary>
    /// Checks if player can spend tokens based on spending limits
    /// </summary>
    Task<bool> CanSpendTokensAsync(Guid playerId, int amount);

    /// <summary>
    /// Gets remaining daily earning capacity for a player
    /// </summary>
    Task<int> GetRemainingDailyEarningCapacityAsync(Guid playerId, Guid? businessId = null);

    /// <summary>
    /// Gets remaining weekly earning capacity for a player
    /// </summary>
    Task<int> GetRemainingWeeklyEarningCapacityAsync(Guid playerId);

    /// <summary>
    /// Gets remaining daily spending capacity for a player
    /// </summary>
    Task<int> GetRemainingDailySpendingCapacityAsync(Guid playerId);

    /// <summary>
    /// Validates if a token earning operation is within limits
    /// </summary>
    Task<TokenLimitValidationResult> ValidateTokenEarningAsync(Guid playerId, int amount, Guid? businessId = null);

    /// <summary>
    /// Validates if a token spending operation is within limits
    /// </summary>
    Task<TokenLimitValidationResult> ValidateTokenSpendingAsync(Guid playerId, int amount);

    /// <summary>
    /// Gets the configured limits for a player
    /// </summary>
    Task<PlayerTokenLimits> GetPlayerLimitsAsync(Guid playerId);
}

public class TokenLimitValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public int RemainingCapacity { get; set; }
    public int RequestedAmount { get; set; }
    public LimitType LimitType { get; set; }

    public static TokenLimitValidationResult Success(int remainingCapacity, int requestedAmount)
    {
        return new TokenLimitValidationResult
        {
            IsValid = true,
            RemainingCapacity = remainingCapacity,
            RequestedAmount = requestedAmount
        };
    }

    public static TokenLimitValidationResult Failure(string errorMessage, int remainingCapacity, int requestedAmount, LimitType limitType)
    {
        return new TokenLimitValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            RemainingCapacity = remainingCapacity,
            RequestedAmount = requestedAmount,
            LimitType = limitType
        };
    }
}

public class PlayerTokenLimits
{
    public int DailyEarningLimit { get; set; } = 100;
    public int WeeklyEarningLimit { get; set; } = 500;
    public int DailySpendingLimit { get; set; } = 200;
    public int DailyEarningLimitPerBusiness { get; set; } = 50;
    public int MaxTokenBalance { get; set; } = 1000;
}

public enum LimitType
{
    DailyEarning,
    WeeklyEarning,
    DailySpending,
    DailyEarningPerBusiness,
    MaxBalance
}