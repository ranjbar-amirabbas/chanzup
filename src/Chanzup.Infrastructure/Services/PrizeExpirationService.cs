using Chanzup.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chanzup.Infrastructure.Services;

public class PrizeExpirationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PrizeExpirationService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

    public PrizeExpirationService(IServiceProvider serviceProvider, ILogger<PrizeExpirationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Prize expiration service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredPrizes();
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during prize expiration cleanup");
                // Wait a shorter time before retrying on error
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Prize expiration service stopped");
    }

    private async Task CleanupExpiredPrizes()
    {
        using var scope = _serviceProvider.CreateScope();
        var prizeExpirationService = scope.ServiceProvider.GetRequiredService<IPrizeExpirationService>();

        try
        {
            var cleanedCount = await prizeExpirationService.CleanupExpiredPrizesAsync();
            
            if (cleanedCount > 0)
            {
                _logger.LogInformation("Automatically cleaned up {Count} expired prizes", cleanedCount);
            }

            // Also send expiration warnings (24 hours before expiry)
            await prizeExpirationService.NotifyPlayersOfExpiringPrizesAsync(TimeSpan.FromHours(24));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired prizes");
        }
    }
}