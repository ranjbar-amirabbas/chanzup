using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Chanzup.Infrastructure.Data;

namespace Chanzup.Infrastructure.Monitoring.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public DatabaseHealthCheck(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple query to check database connectivity
            await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            
            // Check if we can access the main tables
            var businessCount = await _context.Businesses.CountAsync(cancellationToken);
            
            var data = new Dictionary<string, object>
            {
                ["database"] = "connected",
                ["businessCount"] = businessCount,
                ["timestamp"] = DateTime.UtcNow
            };

            return HealthCheckResult.Healthy("Database is healthy", data);
        }
        catch (Exception ex)
        {
            var data = new Dictionary<string, object>
            {
                ["database"] = "disconnected",
                ["error"] = ex.Message,
                ["timestamp"] = DateTime.UtcNow
            };

            return HealthCheckResult.Unhealthy("Database is unhealthy", ex, data);
        }
    }
}