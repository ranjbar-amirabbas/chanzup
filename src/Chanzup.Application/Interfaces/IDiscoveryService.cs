using Chanzup.Application.DTOs;

namespace Chanzup.Application.Interfaces;

public interface IDiscoveryService
{
    Task<NearbyBusinessesResponse> GetNearbyBusinessesAsync(
        double latitude, 
        double longitude, 
        double radiusKm = 5.0,
        string? category = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    Task<BusinessDiscoveryResponse?> GetBusinessDetailsAsync(
        Guid businessId,
        double? searchLatitude = null,
        double? searchLongitude = null,
        CancellationToken cancellationToken = default);

    Task<List<CampaignResponse>> GetActiveCampaignsNearLocationAsync(
        double latitude,
        double longitude,
        double radiusKm = 10.0,
        CancellationToken cancellationToken = default);

    Task<DetailedCampaignResponse?> GetDetailedCampaignAsync(
        Guid campaignId,
        double? searchLatitude = null,
        double? searchLongitude = null,
        CancellationToken cancellationToken = default);

    Task<List<DetailedPrizeInfo>> GetCampaignPrizesAsync(
        Guid campaignId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);
}