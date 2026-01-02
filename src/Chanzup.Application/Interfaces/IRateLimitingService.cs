namespace Chanzup.Application.Interfaces;

public interface IRateLimitingService
{
    /// <summary>
    /// Checks if a request is within rate limits
    /// </summary>
    Task<bool> IsWithinRateLimitAsync(string key, int maxRequests, TimeSpan timeWindow);

    /// <summary>
    /// Records a request for rate limiting tracking
    /// </summary>
    Task RecordRequestAsync(string key);

    /// <summary>
    /// Gets the remaining requests for a given key
    /// </summary>
    Task<int> GetRemainingRequestsAsync(string key, int maxRequests, TimeSpan timeWindow);

    /// <summary>
    /// Resets rate limit for a specific key
    /// </summary>
    Task ResetRateLimitAsync(string key);
}