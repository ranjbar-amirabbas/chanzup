using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Chanzup.Infrastructure.Monitoring.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _redis.GetDatabase();
            
            // Test basic connectivity
            var testKey = "health_check_" + Guid.NewGuid();
            var testValue = DateTime.UtcNow.ToString();
            
            await database.StringSetAsync(testKey, testValue, TimeSpan.FromSeconds(10));
            var retrievedValue = await database.StringGetAsync(testKey);
            await database.KeyDeleteAsync(testKey);

            if (retrievedValue != testValue)
            {
                throw new InvalidOperationException("Redis read/write test failed");
            }

            var info = await _redis.GetServer(_redis.GetEndPoints().First()).InfoAsync();
            var memoryUsage = "unknown";
            
            // Parse Redis info for memory usage
            foreach (var group in info)
            {
                foreach (var item in group)
                {
                    if (item.Key == "used_memory")
                    {
                        memoryUsage = item.Value;
                        break;
                    }
                }
            }

            var data = new Dictionary<string, object>
            {
                ["redis"] = "connected",
                ["memoryUsage"] = memoryUsage,
                ["timestamp"] = DateTime.UtcNow
            };

            return HealthCheckResult.Healthy("Redis is healthy", data);
        }
        catch (Exception ex)
        {
            var data = new Dictionary<string, object>
            {
                ["redis"] = "disconnected",
                ["error"] = ex.Message,
                ["timestamp"] = DateTime.UtcNow
            };

            return HealthCheckResult.Unhealthy("Redis is unhealthy", ex, data);
        }
    }
}