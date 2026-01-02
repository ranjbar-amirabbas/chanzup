using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Chanzup.Application.Interfaces;
using Chanzup.Application.DTOs;

namespace Chanzup.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QRController : ControllerBase
{
    private readonly IQRCodeService _qrCodeService;
    private readonly ICampaignService _campaignService;
    private readonly IQRSessionService _qrSessionService;

    public QRController(IQRCodeService qrCodeService, ICampaignService campaignService, IQRSessionService qrSessionService)
    {
        _qrCodeService = qrCodeService;
        _campaignService = campaignService;
        _qrSessionService = qrSessionService;
    }

    [HttpGet("{campaignId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCampaignQRCode(Guid campaignId)
    {
        // Generate QR code for the campaign
        var qrCodeData = _qrCodeService.GenerateQRCode(campaignId);
        var qrCodeImage = await _qrCodeService.GenerateQRCodeImageAsync(qrCodeData);

        return Ok(new
        {
            campaignId,
            qrCode = qrCodeData,
            qrCodeImage,
            scanUrl = $"{Request.Scheme}://{Request.Host}/api/qr/scan?code={qrCodeData}"
        });
    }

    [HttpGet("{campaignId}/image")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCampaignQRCodeImage(Guid campaignId, [FromQuery] int size = 200)
    {
        var qrCodeData = _qrCodeService.GenerateQRCode(campaignId);
        var qrCodeImage = await _qrCodeService.GenerateQRCodeImageAsync(qrCodeData, size);

        // Extract the base64 data from the data URL
        if (qrCodeImage.StartsWith("data:image/svg+xml;base64,"))
        {
            var base64Data = qrCodeImage.Substring("data:image/svg+xml;base64,".Length);
            var imageBytes = Convert.FromBase64String(base64Data);
            return File(imageBytes, "image/svg+xml");
        }

        return BadRequest("Invalid QR code image format");
    }

    [HttpPost("validate")]
    [AllowAnonymous]
    public IActionResult ValidateQRCode([FromBody] ValidateQRCodeRequest request)
    {
        if (_qrCodeService.ValidateQRCode(request.QRCode, out var campaignId))
        {
            return Ok(new
            {
                valid = true,
                campaignId,
                type = "campaign"
            });
        }

        if (_qrCodeService.ValidateLocationQRCode(request.QRCode, out var businessId, out var locationId))
        {
            return Ok(new
            {
                valid = true,
                businessId,
                locationId,
                type = "location"
            });
        }

        return Ok(new { valid = false });
    }

    [HttpGet("location/{businessId}/{locationId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLocationQRCode(Guid businessId, Guid locationId)
    {
        var qrCodeData = _qrCodeService.GenerateLocationQRCode(businessId, locationId);
        var qrCodeImage = await _qrCodeService.GenerateQRCodeImageAsync(qrCodeData);

        return Ok(new
        {
            businessId,
            locationId,
            qrCode = qrCodeData,
            qrCodeImage,
            scanUrl = $"{Request.Scheme}://{Request.Host}/api/qr/scan?code={qrCodeData}"
        });
    }

    [HttpPost("scan")]
    [Authorize(Roles = "Player")]
    public async Task<IActionResult> ScanQRCode([FromBody] QRScanRequest request)
    {
        try
        {
            // Get player ID from JWT token
            var playerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(playerIdClaim, out var playerId))
            {
                return Unauthorized("Invalid player token");
            }

            var result = await _qrSessionService.ProcessQRScanAsync(playerId, request);

            if (result.SessionId == Guid.Empty)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while processing the QR scan", error = ex.Message });
        }
    }
}

public class ValidateQRCodeRequest
{
    public string QRCode { get; set; } = string.Empty;
}