using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Chanzup.API.Authorization;
using Chanzup.Application.Interfaces;
using Chanzup.Application.DTOs;
using System.Security.Claims;

namespace Chanzup.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CampaignController : ControllerBase
{
    private readonly ICampaignService _campaignService;

    public CampaignController(ICampaignService campaignService)
    {
        _campaignService = campaignService;
    }

    [HttpGet]
    [RequirePermission("campaign:read")]
    public async Task<IActionResult> GetCampaigns()
    {
        var businessId = GetBusinessId();
        if (businessId == null)
            return BadRequest("Business ID not found in token");

        var campaigns = await _campaignService.GetCampaignsAsync(businessId.Value);
        return Ok(campaigns);
    }

    [HttpGet("{id}")]
    [RequirePermission("campaign:read")]
    public async Task<IActionResult> GetCampaign(Guid id)
    {
        var businessId = GetBusinessId();
        if (businessId == null)
            return BadRequest("Business ID not found in token");

        var campaign = await _campaignService.GetCampaignAsync(id, businessId.Value);
        if (campaign == null)
            return NotFound();

        return Ok(campaign);
    }

    [HttpPost]
    [RequirePermission("campaign:write")]
    [RequireTenant]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest request)
    {
        var businessId = GetBusinessId();
        if (businessId == null)
            return BadRequest("Business ID not found in token");

        try
        {
            var campaign = await _campaignService.CreateCampaignAsync(businessId.Value, request);
            return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, campaign);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageCampaigns")]
    [RequireTenant]
    public async Task<IActionResult> UpdateCampaign(Guid id, [FromBody] UpdateCampaignRequest request)
    {
        var businessId = GetBusinessId();
        if (businessId == null)
            return BadRequest("Business ID not found in token");

        var campaign = await _campaignService.UpdateCampaignAsync(id, businessId.Value, request);
        if (campaign == null)
            return NotFound();

        return Ok(campaign);
    }

    [HttpDelete("{id}")]
    [RequireRole("BusinessOwner", "Admin")]
    public async Task<IActionResult> DeleteCampaign(Guid id)
    {
        var businessId = GetBusinessId();
        if (businessId == null)
            return BadRequest("Business ID not found in token");

        var deleted = await _campaignService.DeleteCampaignAsync(id, businessId.Value);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    [HttpPost("{id}/activate")]
    [RequirePermission("campaign:write")]
    public async Task<IActionResult> ActivateCampaign(Guid id)
    {
        var businessId = GetBusinessId();
        if (businessId == null)
            return BadRequest("Business ID not found in token");

        var activated = await _campaignService.ActivateCampaignAsync(id, businessId.Value);
        if (!activated)
            return NotFound();

        return Ok(new { message = "Campaign activated successfully" });
    }

    [HttpPost("{id}/deactivate")]
    [RequirePermission("campaign:write")]
    public async Task<IActionResult> DeactivateCampaign(Guid id)
    {
        var businessId = GetBusinessId();
        if (businessId == null)
            return BadRequest("Business ID not found in token");

        var deactivated = await _campaignService.DeactivateCampaignAsync(id, businessId.Value);
        if (!deactivated)
            return NotFound();

        return Ok(new { message = "Campaign deactivated successfully" });
    }

    [HttpGet("nearby")]
    [AllowAnonymous]
    public async Task<IActionResult> GetNearbyCampaigns([FromQuery] double lat, [FromQuery] double lng, [FromQuery] double radius = 5.0)
    {
        var campaigns = await _campaignService.GetActiveCampaignsNearLocationAsync(lat, lng, radius);
        return Ok(campaigns);
    }

    [HttpGet("analytics")]
    [Authorize(Policy = "CanViewAnalytics")]
    public async Task<IActionResult> GetAnalytics()
    {
        // This endpoint uses policy-based authorization for analytics
        return Ok(new { message = "Analytics data retrieved successfully" });
    }

    private Guid? GetBusinessId()
    {
        var businessIdClaim = User.FindFirst("tenantId")?.Value ?? User.FindFirst("businessId")?.Value;
        return Guid.TryParse(businessIdClaim, out var businessId) ? businessId : null;
    }
}