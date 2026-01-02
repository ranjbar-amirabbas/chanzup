using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Chanzup.Infrastructure.Logging;

public static class LoggingExtensions
{
    public static IHostBuilder ConfigureStructuredLogging(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, configuration) =>
        {
            var environment = context.HostingEnvironment.EnvironmentName;
            
            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Environment", environment)
                .Enrich.WithProperty("Application", "Chanzup.Platform");

            // Console logging for development
            if (context.HostingEnvironment.IsDevelopment())
            {
                configuration.WriteTo.Console();
            }
            else
            {
                // Structured logging for production
                configuration.WriteTo.Console();
            }

            // File logging
            configuration.WriteTo.File(
                path: "logs/chanzup-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30);

            // Optional external logging services can be configured here
            // Application Insights, Seq, etc.
        });
    }

    public static IServiceCollection AddBusinessEventLogging(this IServiceCollection services)
    {
        services.AddScoped<IBusinessEventLogger, BusinessEventLogger>();
        return services;
    }
}

public interface IBusinessEventLogger
{
    void LogQRScan(string playerId, string businessId, int tokensEarned);
    void LogWheelSpin(string playerId, string campaignId, bool wonPrize, string? prizeId = null);
    void LogPrizeRedemption(string playerId, string prizeId, string businessId);
    void LogTokenTransaction(string playerId, int amount, string transactionType, string? relatedId = null);
    void LogSecurityEvent(string eventType, string? userId = null, string? details = null);
    void LogBusinessEvent(string businessId, string eventType, string? details = null);
}

public class BusinessEventLogger : IBusinessEventLogger
{
    private readonly ILogger<BusinessEventLogger> _logger;

    public BusinessEventLogger(ILogger<BusinessEventLogger> logger)
    {
        _logger = logger;
    }

    public void LogQRScan(string playerId, string businessId, int tokensEarned)
    {
        _logger.LogInformation("QR code scanned by player {PlayerId} at business {BusinessId}, earned {TokensEarned} tokens",
            playerId, businessId, tokensEarned);
    }

    public void LogWheelSpin(string playerId, string campaignId, bool wonPrize, string? prizeId = null)
    {
        if (wonPrize && !string.IsNullOrEmpty(prizeId))
        {
            _logger.LogInformation("Player {PlayerId} won prize {PrizeId} in campaign {CampaignId}",
                playerId, prizeId, campaignId);
        }
        else
        {
            _logger.LogInformation("Player {PlayerId} spun wheel in campaign {CampaignId} but did not win",
                playerId, campaignId);
        }
    }

    public void LogPrizeRedemption(string playerId, string prizeId, string businessId)
    {
        _logger.LogInformation("Player {PlayerId} redeemed prize {PrizeId} at business {BusinessId}",
            playerId, prizeId, businessId);
    }

    public void LogTokenTransaction(string playerId, int amount, string transactionType, string? relatedId = null)
    {
        _logger.LogInformation("Token transaction: Player {PlayerId} {TransactionType} {Amount} tokens {RelatedId}",
            playerId, transactionType, Math.Abs(amount), relatedId ?? "");
    }

    public void LogSecurityEvent(string eventType, string? userId = null, string? details = null)
    {
        _logger.LogWarning("Security event: {EventType} for user {UserId} - {Details}",
            eventType, userId ?? "unknown", details ?? "");
    }

    public void LogBusinessEvent(string businessId, string eventType, string? details = null)
    {
        _logger.LogInformation("Business event: {EventType} for business {BusinessId} - {Details}",
            eventType, businessId, details ?? "");
    }
}