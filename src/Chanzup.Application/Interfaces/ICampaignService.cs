using Chanzup.Application.DTOs;

namespace Chanzup.Application.Interfaces;

public interface ICampaignService
{
    Task<CampaignResponse> CreateCampaignAsync(Guid businessId, CreateCampaignRequest request, CancellationToken cancellationToken = default);
    Task<CampaignResponse?> GetCampaignAsync(Guid campaignId, Guid businessId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CampaignResponse>> GetCampaignsAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task<CampaignResponse?> UpdateCampaignAsync(Guid campaignId, Guid businessId, UpdateCampaignRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteCampaignAsync(Guid campaignId, Guid businessId, CancellationToken cancellationToken = default);
    Task<bool> ActivateCampaignAsync(Guid campaignId, Guid businessId, CancellationToken cancellationToken = default);
    Task<bool> DeactivateCampaignAsync(Guid campaignId, Guid businessId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CampaignResponse>> GetActiveCampaignsNearLocationAsync(double latitude, double longitude, double radiusKm = 5.0, CancellationToken cancellationToken = default);
}