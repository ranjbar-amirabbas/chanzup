using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Chanzup.Application.Interfaces;

namespace Chanzup.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscoveryController : ControllerBase
{
    private readonly IDiscoveryService _discoveryService;
    private readonly IMappingService _mappingService;

    public DiscoveryController(IDiscoveryService discoveryService, IMappingService mappingService)
    {
        _discoveryService = discoveryService;
        _mappingService = mappingService;
    }

    [HttpGet("nearby")]
    [AllowAnonymous]
    public async Task<IActionResult> GetNearbyBusinesses(
        [FromQuery] double lat, 
        [FromQuery] double lng, 
        [FromQuery] double radius = 5.0,
        [FromQuery] string? category = null,
        [FromQuery] bool activeOnly = true)
    {
        try
        {
            var result = await _discoveryService.GetNearbyBusinessesAsync(
                lat, lng, radius, category, activeOnly);
            
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while searching for nearby businesses" });
        }
    }

    [HttpGet("businesses/{businessId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBusinessDetails(
        Guid businessId,
        [FromQuery] double? lat = null,
        [FromQuery] double? lng = null)
    {
        try
        {
            var result = await _discoveryService.GetBusinessDetailsAsync(businessId, lat, lng);
            
            if (result == null)
                return NotFound(new { error = "Business not found" });
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving business details" });
        }
    }

    [HttpGet("campaigns/active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveCampaigns(
        [FromQuery] double lat, 
        [FromQuery] double lng, 
        [FromQuery] double radius = 10.0)
    {
        try
        {
            var campaigns = await _discoveryService.GetActiveCampaignsNearLocationAsync(lat, lng, radius);
            
            var result = campaigns.Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.GameType,
                c.TokenCostPerSpin,
                c.MaxSpinsPerDay,
                c.QRCodeUrl,
                Business = c.Business != null ? new
                {
                    c.Business.Id,
                    c.Business.Name,
                    c.Business.Address,
                    c.Business.DistanceKm
                } : null,
                AvailablePrizes = c.Prizes.Count(p => p.IsActive && p.RemainingQuantity > 0),
                TotalPrizeValue = c.Prizes
                    .Where(p => p.IsActive && p.RemainingQuantity > 0)
                    .Sum(p => (p.Value ?? 0) * p.RemainingQuantity),
                TopPrize = c.Prizes
                    .Where(p => p.IsActive && p.RemainingQuantity > 0)
                    .OrderByDescending(p => p.Value ?? 0)
                    .FirstOrDefault()
            });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving active campaigns" });
        }
    }

    [HttpGet("campaigns/{campaignId}/details")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCampaignDetails(
        Guid campaignId,
        [FromQuery] double? lat = null,
        [FromQuery] double? lng = null)
    {
        try
        {
            var result = await _discoveryService.GetDetailedCampaignAsync(campaignId, lat, lng);
            
            if (result == null)
                return NotFound(new { error = "Campaign not found or not active" });
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving campaign details" });
        }
    }

    [HttpGet("campaigns/{campaignId}/prizes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCampaignPrizes(
        Guid campaignId,
        [FromQuery] bool activeOnly = true)
    {
        try
        {
            var prizes = await _discoveryService.GetCampaignPrizesAsync(campaignId, activeOnly);
            
            return Ok(new
            {
                campaignId,
                activeOnly,
                totalPrizes = prizes.Count,
                prizes
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving campaign prizes" });
        }
    }

    [HttpGet("navigation/business/{businessId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetNavigationToBusiness(
        Guid businessId,
        [FromQuery] double originLat,
        [FromQuery] double originLng,
        [FromQuery] string travelMode = "walking")
    {
        try
        {
            var navigation = await _mappingService.GetNavigationAsync(
                originLat, originLng, businessId, travelMode);
            
            return Ok(navigation);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while generating navigation" });
        }
    }

    [HttpGet("navigation/location")]
    [AllowAnonymous]
    public async Task<IActionResult> GetNavigationToLocation(
        [FromQuery] double originLat,
        [FromQuery] double originLng,
        [FromQuery] double destLat,
        [FromQuery] double destLng,
        [FromQuery] string travelMode = "walking")
    {
        try
        {
            var navigation = await _mappingService.GetNavigationToLocationAsync(
                originLat, originLng, destLat, destLng, travelMode);
            
            return Ok(navigation);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while generating navigation" });
        }
    }

    [HttpGet("map/embed")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMapEmbed(
        [FromQuery] double centerLat,
        [FromQuery] double centerLng,
        [FromQuery] string? businessIds = null,
        [FromQuery] int zoomLevel = 15,
        [FromQuery] int width = 400,
        [FromQuery] int height = 300)
    {
        try
        {
            List<Guid>? businessIdList = null;
            if (!string.IsNullOrEmpty(businessIds))
            {
                businessIdList = businessIds.Split(',')
                    .Where(id => Guid.TryParse(id.Trim(), out _))
                    .Select(Guid.Parse)
                    .ToList();
            }

            var mapEmbed = await _mappingService.GetMapEmbedAsync(
                centerLat, centerLng, businessIdList, zoomLevel, width, height);
            
            return Ok(mapEmbed);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while generating map embed" });
        }
    }

    [HttpGet("distance")]
    [AllowAnonymous]
    public async Task<IActionResult> CalculateDistance(
        [FromQuery] double lat1,
        [FromQuery] double lng1,
        [FromQuery] double lat2,
        [FromQuery] double lng2)
    {
        try
        {
            var distance = await _mappingService.CalculateDistanceAsync(lat1, lng1, lat2, lng2);
            
            return Ok(new
            {
                origin = new { latitude = lat1, longitude = lng1 },
                destination = new { latitude = lat2, longitude = lng2 },
                distanceKm = distance,
                googleMapsUrl = _mappingService.GenerateGoogleMapsUrl(lat1, lng1, lat2, lng2),
                appleMapsUrl = _mappingService.GenerateAppleMapsUrl(lat1, lng1, lat2, lng2)
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while calculating distance" });
        }
    }
}