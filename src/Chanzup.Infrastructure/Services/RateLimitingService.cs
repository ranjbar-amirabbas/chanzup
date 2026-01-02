using Microsoft.Extensions.Caching.Memory;
using Chanzup.Application.Interfaces;

namespace Chanzup.Infrastructure.Services;

public class RateLimitingService : IRateLimitingService
{
    private readonly IMemoryCache _cache;
    private readonly object _lock = new();

    public RateLimitingService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<bool> IsWithinRateLimitAsync(string key, int maxRequests, TimeSpan timeWindow)
    {
        await Task.CompletedTask;
        
        lock (_lock)
        {
            var cacheKey = $"rate_limit:{key}";
            
            if (!_cache.TryGetValue(cacheKey, out List<DateTime>? requests))
            {
                return true; // No previous requests
            }

            // Remove expired requests
            var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
            requests = requests!.Where(r => r > cutoffTime).ToList();
            
            // Update cache with cleaned list
            _cache.Set(cacheKey, requests, timeWindow);

            return requests.Count < maxRequests;
        }
    }

    public async Task RecordRequestAsync(string key)
    {
        await Task.CompletedTask;
        
        lock (_lock)
        {
            var cacheKey = $"rate_limit:{key}";
            var now = DateTime.UtcNow;
            
            if (!_cache.TryGetValue(cacheKey, out List<DateTime>? requests))
            {
                requests = new List<DateTime>();
            }

            requests!.Add(now);
            
            // Set cache with sliding expiration
            _cache.Set(cacheKey, requests, TimeSpan.FromHours(1));
        }
    }

    public async Task<int> GetRemainingRequestsAsync(string key, int maxRequests, TimeSpan timeWindow)
    {
        await Task.CompletedTask;
        
        lock (_lock)
        {
            var cacheKey = $"rate_limit:{key}";
            
            if (!_cache.TryGetValue(cacheKey, out List<DateTime>? requests))
            {
                return maxRequests;
            }

            // Remove expired requests
            var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
            requests = requests!.Where(r => r > cutoffTime).ToList();
            
            return Math.Max(0, maxRequests - requests.Count);
        }
    }

    public async Task ResetRateLimitAsync(string key)
    {
        await Task.CompletedTask;
        
        var cacheKey = $"rate_limit:{key}";
        _cache.Remove(cacheKey);
    }
}