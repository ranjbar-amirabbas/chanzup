namespace Chanzup.Domain.Services;

public interface IQRCodeService
{
    string GenerateQRCode(Guid campaignId, Guid businessId);
    bool ValidateQRCode(string qrCode, out Guid campaignId, out Guid businessId);
    string GenerateQRCodeImage(string qrCode);
}