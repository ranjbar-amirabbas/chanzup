namespace Chanzup.Application.Interfaces;

public interface IQRCodeService
{
    /// <summary>
    /// Generates a unique QR code string for a campaign
    /// </summary>
    string GenerateQRCode(Guid campaignId);

    /// <summary>
    /// Generates a QR code image as base64 string
    /// </summary>
    Task<string> GenerateQRCodeImageAsync(string qrCodeData, int size = 200);

    /// <summary>
    /// Validates a QR code format and extracts campaign ID
    /// </summary>
    bool ValidateQRCode(string qrCode, out Guid campaignId);

    /// <summary>
    /// Generates a unique QR code for business location
    /// </summary>
    string GenerateLocationQRCode(Guid businessId, Guid locationId);

    /// <summary>
    /// Validates location QR code and extracts business and location IDs
    /// </summary>
    bool ValidateLocationQRCode(string qrCode, out Guid businessId, out Guid locationId);
}