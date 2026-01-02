using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Chanzup.Application.Interfaces;
using Chanzup.Application.Services;
using Chanzup.Infrastructure.Data;
using Chanzup.Infrastructure.Services;
using Chanzup.Infrastructure.Monitoring.HealthChecks;
using Chanzup.Infrastructure.Monitoring.Metrics;
using StackExchange.Redis;

namespace Chanzup.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Multi-tenant context
        services.AddScoped<ITenantContext, TenantContext>();

        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (connectionString?.Contains(".db") == true)
        {
            // Use SQLite for local development
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connectionString));
        }
        else
        {
            // Use SQL Server for production
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
        }

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Application Services
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<IDiscoveryService, DiscoveryService>();
        services.AddScoped<IMappingService, MappingService>();
        services.AddScoped<IQRSessionService, QRSessionService>();
        services.AddScoped<IAntiFraudService, AntiFraudService>();
        services.AddScoped<ITokenTransactionService, TokenTransactionService>();
        services.AddScoped<ITokenLimitService, TokenLimitService>();
        services.AddScoped<IReferralService, ReferralService>();
        services.AddScoped<IRedemptionService, RedemptionService>();
        services.AddScoped<IPrizeExpirationService, Application.Services.PrizeExpirationService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IAdminManagementService, AdminManagementService>();
        services.AddScoped<IFraudDetectionService, FraudDetectionService>();
        services.AddScoped<ISystemParameterService, SystemParameterService>();
        services.AddScoped<ISecurityService, SecurityService>();

        // Infrastructure Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IQRCodeService, QRCodeService>();
        services.AddScoped<IRateLimitingService, RateLimitingService>();

        // Background Services
        services.AddHostedService<Infrastructure.Services.PrizeExpirationService>();

        // Add memory cache for rate limiting
        services.AddMemoryCache();

        // Add HttpClient for external API calls
        services.AddHttpClient();

        // Add Redis if connection string is provided
        var redisConnectionString = configuration.GetConnectionString("RedisConnection");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(provider =>
                ConnectionMultiplexer.Connect(redisConnectionString));
        }

        // Add monitoring services
        services.AddSingleton<ApplicationMetrics>();
        
        // Add health checks
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready", "live" })
            .AddCheck<ExternalServicesHealthCheck>("external_services", tags: new[] { "ready" });

        // Add Redis health check if Redis is configured
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddHealthChecks()
                .AddCheck<RedisHealthCheck>("redis", tags: new[] { "ready", "live" });
        }

        return services;
    }
}