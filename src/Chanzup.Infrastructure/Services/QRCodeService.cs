using System.Text;
using System.Text.RegularExpressions;
using Chanzup.Application.Interfaces;

namespace Chanzup.Infrastructure.Services;

public class QRCodeService : IQRCodeService
{
    private const string CAMPAIGN_PREFIX = "CMP";
    private const string LOCATION_PREFIX = "LOC";
    private const string QR_SEPARATOR = "-";

    public string GenerateQRCode(Guid campaignId)
    {
        // Format: CMP-{campaignId}
        return $"{CAMPAIGN_PREFIX}{QR_SEPARATOR}{campaignId:N}";
    }

    public async Task<string> GenerateQRCodeImageAsync(string qrCodeData, int size = 200)
    {
        // For now, return a placeholder base64 image
        // In production, you would use a QR code library like QRCoder or ZXing.Net
        await Task.Delay(1); // Simulate async operation
        
        // This is a placeholder - in real implementation, generate actual QR code image
        var placeholder = $"data:image/svg+xml;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(GeneratePlaceholderSvg(qrCodeData, size)))}";
        return placeholder;
    }

    public bool ValidateQRCode(string qrCode, out Guid campaignId)
    {
        campaignId = Guid.Empty;

        if (string.IsNullOrWhiteSpace(qrCode))
            return false;

        // Expected format: CMP-{guid}
        var pattern = $@"^{CAMPAIGN_PREFIX}{QR_SEPARATOR}([a-fA-F0-9]{{32}})$";
        var match = Regex.Match(qrCode, pattern);

        if (!match.Success)
            return false;

        var guidString = match.Groups[1].Value;
        
        // Insert hyphens to make it a proper GUID format
        var formattedGuid = $"{guidString.Substring(0, 8)}-{guidString.Substring(8, 4)}-{guidString.Substring(12, 4)}-{guidString.Substring(16, 4)}-{guidString.Substring(20, 12)}";
        
        return Guid.TryParse(formattedGuid, out campaignId);
    }

    public string GenerateLocationQRCode(Guid businessId, Guid locationId)
    {
        // Format: LOC-{businessId}-{locationId}
        return $"{LOCATION_PREFIX}{QR_SEPARATOR}{businessId:N}{QR_SEPARATOR}{locationId:N}";
    }

    public bool ValidateLocationQRCode(string qrCode, out Guid businessId, out Guid locationId)
    {
        businessId = Guid.Empty;
        locationId = Guid.Empty;

        if (string.IsNullOrWhiteSpace(qrCode))
            return false;

        // Expected format: LOC-{businessGuid}-{locationGuid}
        var pattern = $@"^{LOCATION_PREFIX}{QR_SEPARATOR}([a-fA-F0-9]{{32}}){QR_SEPARATOR}([a-fA-F0-9]{{32}})$";
        var match = Regex.Match(qrCode, pattern);

        if (!match.Success)
            return false;

        var businessGuidString = match.Groups[1].Value;
        var locationGuidString = match.Groups[2].Value;

        // Format GUIDs properly
        var formattedBusinessGuid = FormatGuidString(businessGuidString);
        var formattedLocationGuid = FormatGuidString(locationGuidString);

        return Guid.TryParse(formattedBusinessGuid, out businessId) && 
               Guid.TryParse(formattedLocationGuid, out locationId);
    }

    private static string FormatGuidString(string guidString)
    {
        return $"{guidString.Substring(0, 8)}-{guidString.Substring(8, 4)}-{guidString.Substring(12, 4)}-{guidString.Substring(16, 4)}-{guidString.Substring(20, 12)}";
    }

    private static string GeneratePlaceholderSvg(string data, int size)
    {
        return $@"<svg width=""{size}"" height=""{size}"" xmlns=""http://www.w3.org/2000/svg"">
            <rect width=""{size}"" height=""{size}"" fill=""white"" stroke=""black"" stroke-width=""2""/>
            <text x=""{size / 2}"" y=""{size / 2}"" text-anchor=""middle"" dominant-baseline=""middle"" font-family=""monospace"" font-size=""12"" fill=""black"">
                QR: {data.Substring(0, Math.Min(data.Length, 20))}...
            </text>
        </svg>";
    }
}