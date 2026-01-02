using Chanzup.API.Authorization;
using Chanzup.Application.DTOs;
using Chanzup.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Chanzup.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RedemptionController : ControllerBase
{
    private readonly IRedemptionService _redemptionService;
    private readonly ILogger<RedemptionController> _logger;

    public RedemptionController(IRedemptionService redemptionService, ILogger<RedemptionController> logger)
    {
        _redemptionService = redemptionService;
        _logger = logger;
    }

    /// <summary>
    /// Verify a redemption code without completing the redemption
    /// </summary>
    [HttpPost("verify")]
    [RequirePermission("redemption:verify")]
    public async Task<ActionResult<RedemptionVerificationResponse>> VerifyRedemptionCode([FromBody] RedemptionVerificationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _redemptionService.VerifyRedemptionCodeAsync(request.RedemptionCode);
        
        if (!result.IsValid)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Complete a prize redemption
    /// </summary>
    [HttpPost("complete")]
    [RequirePermission("redemption:verify")]
    public async Task<ActionResult<RedemptionCompletionResponse>> CompleteRedemption([FromBody] RedemptionCompletionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Get staff ID from claims or use the provided one (for admin override)
        var staffIdClaim = User.FindFirst("sub")?.Value;
        var staffId = request.StaffId;
        
        // If not admin, ensure staff can only redeem for themselves
        if (!User.IsInRole("Admin") && staffIdClaim != null)
        {
            if (!Guid.TryParse(staffIdClaim, out var claimStaffId) || claimStaffId != request.StaffId)
            {
                return Forbid("Staff can only complete redemptions for themselves");
            }
        }

        var result = await _redemptionService.CompleteRedemptionAsync(request.RedemptionCode, staffId);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get player's prizes (for player wallet)
    /// </summary>
    [HttpGet("player/{playerId}/prizes")]
    [RequirePermission("prize:read")]
    public async Task<ActionResult<IEnumerable<PlayerPrizeResponse>>> GetPlayerPrizes(
        Guid playerId, 
        [FromQuery] bool includeRedeemed = false)
    {
        // Players can only view their own prizes unless admin/staff
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (!User.IsInRole("Admin") && !User.IsInRole("Staff"))
        {
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId) || userId != playerId)
            {
                return Forbid("Players can only view their own prizes");
            }
        }

        var prizes = await _redemptionService.GetPlayerPrizesAsync(playerId, includeRedeemed);
        return Ok(prizes);
    }

    /// <summary>
    /// Get redemption statistics for a business
    /// </summary>
    [HttpGet("stats/{businessId}")]
    [RequirePermission("analytics:read")]
    public async Task<ActionResult<RedemptionStatsResponse>> GetRedemptionStats(
        Guid businessId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        // Business owners can only view their own stats unless admin
        var tenantIdClaim = User.FindFirst("tenantId")?.Value;
        if (!User.IsInRole("Admin"))
        {
            if (tenantIdClaim == null || !Guid.TryParse(tenantIdClaim, out var tenantId) || tenantId != businessId)
            {
                return Forbid("Business owners can only view their own redemption statistics");
            }
        }

        var stats = await _redemptionService.GetRedemptionStatsAsync(businessId, startDate, endDate);
        return Ok(stats);
    }

    /// <summary>
    /// Cleanup expired prizes (admin only)
    /// </summary>
    [HttpPost("cleanup-expired")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<int>> CleanupExpiredPrizes()
    {
        var cleanedCount = await _redemptionService.CleanupExpiredPrizesAsync();
        
        _logger.LogInformation("Admin {AdminId} triggered cleanup of {Count} expired prizes", 
            User.FindFirst("sub")?.Value, cleanedCount);
            
        return Ok(new { CleanedCount = cleanedCount, Message = $"Cleaned up {cleanedCount} expired prizes" });
    }

    /// <summary>
    /// Cleanup expired prizes for a specific business (business owner or admin)
    /// </summary>
    [HttpPost("cleanup-expired/{businessId}")]
    [RequirePermission("analytics:read")]
    public async Task<ActionResult<int>> CleanupExpiredPrizesForBusiness(Guid businessId)
    {
        // Business owners can only cleanup their own expired prizes unless admin
        var tenantIdClaim = User.FindFirst("tenantId")?.Value;
        if (!User.IsInRole("Admin"))
        {
            if (tenantIdClaim == null || !Guid.TryParse(tenantIdClaim, out var tenantId) || tenantId != businessId)
            {
                return Forbid("Business owners can only cleanup their own expired prizes");
            }
        }

        using var scope = HttpContext.RequestServices.CreateScope();
        var prizeExpirationService = scope.ServiceProvider.GetRequiredService<IPrizeExpirationService>();
        
        var cleanedCount = await prizeExpirationService.CleanupExpiredPrizesForBusinessAsync(businessId);
        
        _logger.LogInformation("User {UserId} triggered cleanup of {Count} expired prizes for business {BusinessId}", 
            User.FindFirst("sub")?.Value, cleanedCount, businessId);
            
        return Ok(new { CleanedCount = cleanedCount, Message = $"Cleaned up {cleanedCount} expired prizes for business" });
    }
}