using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Chanzup.Application.Interfaces;
using Chanzup.API.Authorization;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

namespace Chanzup.API.Controllers;

/// <summary>
/// Business management and dashboard endpoints
/// </summary>
/// <remarks>
/// This controller provides business-specific functionality including:
/// - Business information retrieval
/// - Dashboard metrics and analytics
/// - Business settings management
/// 
/// All endpoints require business staff authentication (Owner, Manager, or Staff roles).
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireRole("BusinessOwner", "Staff")]
[Produces("application/json")]
[SwaggerTag("Business management and dashboard operations")]
public class BusinessController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public BusinessController(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves detailed information about the authenticated user's business
    /// </summary>
    /// <returns>Business details including name, contact info, subscription tier, and status</returns>
    /// <response code="200">Business information retrieved successfully</response>
    /// <response code="400">Invalid tenant information</response>
    /// <response code="401">Unauthorized - Invalid or missing JWT token</response>
    /// <response code="403">Forbidden - Insufficient permissions</response>
    /// <response code="404">Business not found</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// This endpoint returns comprehensive business information for the authenticated staff member's business.
    /// The business ID is automatically extracted from the JWT token's tenant claim.
    /// 
    /// **Required Role:** BusinessOwner or Staff
    /// </remarks>
    [HttpGet("info")]
    [SwaggerOperation(
        Summary = "Get business information",
        Description = "Retrieves detailed information about the authenticated user's business including subscription details and contact information.",
        OperationId = "GetBusinessInfo",
        Tags = new[] { "🏢 Business Management" }
    )]
    [SwaggerResponse(200, "Business information retrieved successfully", typeof(object))]
    [SwaggerResponse(400, "Invalid tenant information", typeof(object))]
    [SwaggerResponse(401, "Unauthorized", typeof(object))]
    [SwaggerResponse(403, "Forbidden", typeof(object))]
    [SwaggerResponse(404, "Business not found", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<IActionResult> GetBusinessInfo()
    {
        try
        {
            var tenantIdClaim = User.FindFirst("tenantId")?.Value;
            if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                return BadRequest(new { error = "Invalid tenant information" });
            }

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == tenantId);

            if (business == null)
            {
                return NotFound(new { error = "Business not found" });
            }

            return Ok(new
            {
                id = business.Id,
                name = business.Name,
                email = business.Email.Value,
                phone = business.Phone,
                address = business.Address,
                subscriptionTier = business.SubscriptionTier,
                isActive = business.IsActive,
                createdAt = business.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves dashboard metrics and analytics for the authenticated business
    /// </summary>
    /// <returns>Key performance indicators and metrics for business dashboard</returns>
    /// <response code="200">Dashboard metrics retrieved successfully</response>
    /// <response code="400">Invalid tenant information</response>
    /// <response code="401">Unauthorized - Invalid or missing JWT token</response>
    /// <response code="403">Forbidden - Insufficient permissions</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// This endpoint provides real-time dashboard metrics including:
    /// - Active campaigns count
    /// - Today's game spins
    /// - Prizes redeemed
    /// - Unique players engaged
    /// - Token revenue generated
    /// - Overall redemption rate
    /// 
    /// **Required Role:** BusinessOwner or Staff
    /// **Required Permission:** analytics:read
    /// </remarks>
    [HttpGet("dashboard/metrics")]
    [SwaggerOperation(
        Summary = "Get dashboard metrics",
        Description = "Retrieves key performance indicators and analytics data for the business dashboard including campaign performance, player engagement, and revenue metrics.",
        OperationId = "GetDashboardMetrics",
        Tags = new[] { "🏢 Business Management" }
    )]
    [SwaggerResponse(200, "Dashboard metrics retrieved successfully", typeof(object))]
    [SwaggerResponse(400, "Invalid tenant information", typeof(object))]
    [SwaggerResponse(401, "Unauthorized", typeof(object))]
    [SwaggerResponse(403, "Forbidden", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<IActionResult> GetDashboardMetrics()
    {
        try
        {
            var tenantIdClaim = User.FindFirst("tenantId")?.Value;
            if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                return BadRequest(new { error = "Invalid tenant information" });
            }

            var today = DateTime.UtcNow.Date;

            // Get active campaigns count
            var activeCampaigns = await _context.Campaigns
                .CountAsync(c => c.BusinessId == tenantId && c.IsActive);

            // Get today's spins count
            var spinsToday = await _context.WheelSpins
                .Join(_context.Campaigns, ws => ws.CampaignId, c => c.Id, (ws, c) => new { ws, c })
                .CountAsync(x => x.c.BusinessId == tenantId && x.ws.CreatedAt.Date == today);

            // Get redeemed prizes count
            var prizesRedeemed = await _context.PlayerPrizes
                .Join(_context.Prizes, pp => pp.PrizeId, p => p.Id, (pp, p) => new { pp, p })
                .Join(_context.Campaigns, x => x.p.CampaignId, c => c.Id, (x, c) => new { x.pp, x.p, c })
                .CountAsync(x => x.c.BusinessId == tenantId && x.pp.IsRedeemed);

            // Get unique players count
            var totalPlayers = await _context.QRSessions
                .Join(_context.Campaigns, qs => qs.CampaignId, c => c.Id, (qs, c) => new { qs, c })
                .Where(x => x.c.BusinessId == tenantId)
                .Select(x => x.qs.PlayerId)
                .Distinct()
                .CountAsync();

            // Calculate token revenue (simplified)
            var tokenRevenue = await _context.TokenTransactions
                .Join(_context.QRSessions, tt => tt.PlayerId, qs => qs.PlayerId, (tt, qs) => new { tt, qs })
                .Join(_context.Campaigns, x => x.qs.CampaignId, c => c.Id, (x, c) => new { x.tt, x.qs, c })
                .Where(x => x.c.BusinessId == tenantId && x.tt.TransactionType == TransactionType.Purchase)
                .SumAsync(x => x.tt.Amount);

            // Calculate redemption rate
            var totalPrizesWon = await _context.PlayerPrizes
                .Join(_context.Prizes, pp => pp.PrizeId, p => p.Id, (pp, p) => new { pp, p })
                .Join(_context.Campaigns, x => x.p.CampaignId, c => c.Id, (x, c) => new { x.pp, x.p, c })
                .CountAsync(x => x.c.BusinessId == tenantId);

            var redemptionRate = totalPrizesWon > 0 ? (double)prizesRedeemed / totalPrizesWon : 0;

            return Ok(new
            {
                activeCampaigns,
                totalSpinsToday = spinsToday,
                prizesRedeemed,
                totalPlayers,
                tokenRevenue,
                redemptionRate
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }
}