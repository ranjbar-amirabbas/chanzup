using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Chanzup.Infrastructure.Monitoring.Metrics;
using Chanzup.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Chanzup.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class MetricsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ApplicationMetrics _metrics;

    public MetricsController(ApplicationDbContext context, ApplicationMetrics metrics)
    {
        _context = context;
        _metrics = metrics;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var summary = new
        {
            timestamp = DateTime.UtcNow,
            platform = new
            {
                totalBusinesses = await _context.Businesses.CountAsync(),
                activeBusinesses = await _context.Businesses.CountAsync(b => b.IsActive),
                totalPlayers = await _context.Players.CountAsync(),
                activePlayers = await _context.Players.CountAsync(p => p.IsActive),
                totalCampaigns = await _context.Campaigns.CountAsync(),
                activeCampaigns = await _context.Campaigns.CountAsync(c => c.IsActive && 
                    c.StartDate <= DateTime.UtcNow && 
                    (c.EndDate == null || c.EndDate >= DateTime.UtcNow))
            },
            activity = new
            {
                totalQrScans = await _context.QRSessions.CountAsync(),
                totalWheelSpins = await _context.WheelSpins.CountAsync(),
                totalPrizesWon = await _context.PlayerPrizes.CountAsync(),
                totalRedemptions = await _context.PlayerPrizes.CountAsync(p => p.IsRedeemed),
                todayQrScans = await _context.QRSessions.CountAsync(q => q.CreatedAt.Date == DateTime.UtcNow.Date),
                todayWheelSpins = await _context.WheelSpins.CountAsync(w => w.CreatedAt.Date == DateTime.UtcNow.Date)
            },
            tokens = new
            {
                totalTokensInCirculation = await _context.Players.SumAsync(p => p.TokenBalance),
                totalTokenTransactions = await _context.TokenTransactions.CountAsync(),
                todayTokensEarned = await _context.TokenTransactions
                    .Where(t => t.CreatedAt.Date == DateTime.UtcNow.Date && t.Amount > 0)
                    .SumAsync(t => t.Amount),
                todayTokensSpent = await _context.TokenTransactions
                    .Where(t => t.CreatedAt.Date == DateTime.UtcNow.Date && t.Amount < 0)
                    .SumAsync(t => Math.Abs(t.Amount))
            }
        };

        return Ok(summary);
    }

    [HttpGet("performance")]
    public async Task<IActionResult> GetPerformance()
    {
        var performance = new
        {
            timestamp = DateTime.UtcNow,
            database = new
            {
                averageResponseTime = "N/A", // Would be calculated from metrics
                connectionPoolSize = "N/A",
                activeConnections = "N/A"
            },
            api = new
            {
                averageResponseTime = "N/A", // Would be calculated from metrics
                requestsPerMinute = "N/A",
                errorRate = "N/A"
            },
            cache = new
            {
                hitRate = "N/A", // Would be calculated from Redis metrics
                memoryUsage = "N/A"
            }
        };

        return Ok(performance);
    }

    [HttpPost("refresh-gauges")]
    public async Task<IActionResult> RefreshGauges()
    {
        // Update gauge metrics with current values
        var activePlayers = await _context.Players.CountAsync(p => p.IsActive);
        var activeCampaigns = await _context.Campaigns.CountAsync(c => c.IsActive && 
            c.StartDate <= DateTime.UtcNow && 
            (c.EndDate == null || c.EndDate >= DateTime.UtcNow));

        _metrics.SetActivePlayers(activePlayers);
        _metrics.SetActiveCampaigns(activeCampaigns);

        return Ok(new { message = "Gauges refreshed", activePlayers, activeCampaigns });
    }
}