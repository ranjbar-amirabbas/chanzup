using Chanzup.Application.DTOs;

namespace Chanzup.Application.Interfaces;

public interface IMappingService
{
    Task<NavigationResponse> GetNavigationAsync(
        double originLat,
        double originLng,
        Guid businessId,
        string travelMode = "walking",
        CancellationToken cancellationToken = default);

    Task<NavigationResponse> GetNavigationToLocationAsync(
        double originLat,
        double originLng,
        double destLat,
        double destLng,
        string travelMode = "walking",
        CancellationToken cancellationToken = default);

    Task<MapEmbedResponse> GetMapEmbedAsync(
        double centerLat,
        double centerLng,
        List<Guid>? businessIds = null,
        int zoomLevel = 15,
        int width = 400,
        int height = 300,
        CancellationToken cancellationToken = default);

    string GenerateGoogleMapsUrl(double originLat, double originLng, double destLat, double destLng);
    string GenerateAppleMapsUrl(double originLat, double originLng, double destLat, double destLng);
    
    Task<double> CalculateDistanceAsync(
        double lat1, double lng1, 
        double lat2, double lng2,
        CancellationToken cancellationToken = default);
}