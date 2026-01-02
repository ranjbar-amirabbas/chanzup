using Microsoft.EntityFrameworkCore;
using Chanzup.Application.DTOs;
using Chanzup.Application.Interfaces;
using Chanzup.Domain.ValueObjects;

namespace Chanzup.Application.Services;

public class MappingService : IMappingService
{
    private readonly IApplicationDbContext _context;

    public MappingService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NavigationResponse> GetNavigationAsync(
        double originLat,
        double originLng,
        Guid businessId,
        string travelMode = "walking",
        CancellationToken cancellationToken = default)
    {
        var business = await _context.Businesses
            .FirstOrDefaultAsync(b => b.Id == businessId && b.IsActive, cancellationToken);

        if (business == null || business.Location == null)
            throw new ArgumentException("Business not found or has no location", nameof(businessId));

        var destLat = (double)business.Location.Latitude;
        var destLng = (double)business.Location.Longitude;

        var navigationInfo = await GetNavigationToLocationAsync(originLat, originLng, destLat, destLng, travelMode, cancellationToken);
        
        // Update business info in response
        navigationInfo.Business = new BusinessInfo
        {
            Id = business.Id,
            Name = business.Name,
            Address = business.Address,
            Latitude = business.Location.Latitude,
            Longitude = business.Location.Longitude
        };

        return navigationInfo;
    }

    public async Task<NavigationResponse> GetNavigationToLocationAsync(
        double originLat,
        double originLng,
        double destLat,
        double destLng,
        string travelMode = "walking",
        CancellationToken cancellationToken = default)
    {
        // Validate coordinates
        ValidateCoordinates(originLat, originLng);
        ValidateCoordinates(destLat, destLng);

        var distance = await CalculateDistanceAsync(originLat, originLng, destLat, destLng, cancellationToken);
        var estimatedTime = CalculateEstimatedTravelTime(distance, travelMode);

        // Generate basic navigation steps (simplified)
        var steps = GenerateBasicNavigationSteps(originLat, originLng, destLat, destLng, travelMode);

        var response = new NavigationResponse
        {
            Origin = new LocationInfo { Latitude = (decimal)originLat, Longitude = (decimal)originLng },
            Destination = new LocationInfo { Latitude = (decimal)destLat, Longitude = (decimal)destLng },
            Navigation = new NavigationInfo
            {
                DistanceKm = distance,
                EstimatedTravelTimeMinutes = estimatedTime,
                TravelMode = travelMode,
                Steps = steps,
                GoogleMapsUrl = GenerateGoogleMapsUrl(originLat, originLng, destLat, destLng),
                AppleMapsUrl = GenerateAppleMapsUrl(originLat, originLng, destLat, destLng),
                Route = new RouteInfo
                {
                    Polyline = GenerateSimplePolyline(originLat, originLng, destLat, destLng),
                    EncodedPolyline = "", // Would be populated by actual mapping service
                    Bounds = new RouteBounds
                    {
                        Northeast = new LocationInfo 
                        { 
                            Latitude = (decimal)Math.Max(originLat, destLat), 
                            Longitude = (decimal)Math.Max(originLng, destLng) 
                        },
                        Southwest = new LocationInfo 
                        { 
                            Latitude = (decimal)Math.Min(originLat, destLat), 
                            Longitude = (decimal)Math.Min(originLng, destLng) 
                        }
                    },
                    Warnings = new List<string>()
                }
            }
        };

        return response;
    }

    public async Task<MapEmbedResponse> GetMapEmbedAsync(
        double centerLat,
        double centerLng,
        List<Guid>? businessIds = null,
        int zoomLevel = 15,
        int width = 400,
        int height = 300,
        CancellationToken cancellationToken = default)
    {
        ValidateCoordinates(centerLat, centerLng);

        var markers = new List<MapMarker>();

        // Add center marker
        markers.Add(new MapMarker
        {
            Location = new LocationInfo { Latitude = (decimal)centerLat, Longitude = (decimal)centerLng },
            Label = "You",
            Color = "blue",
            Title = "Your Location",
            Description = "Current location"
        });

        // Add business markers if specified
        if (businessIds != null && businessIds.Any())
        {
            var businesses = await _context.Businesses
                .Where(b => businessIds.Contains(b.Id) && b.IsActive && b.Location != null)
                .ToListAsync(cancellationToken);

            foreach (var business in businesses)
            {
                markers.Add(new MapMarker
                {
                    Location = new LocationInfo 
                    { 
                        Latitude = business.Location!.Latitude, 
                        Longitude = business.Location.Longitude 
                    },
                    Label = business.Name.Substring(0, Math.Min(business.Name.Length, 10)),
                    Color = "red",
                    Title = business.Name,
                    Description = business.Address ?? "Business location"
                });
            }
        }

        var response = new MapEmbedResponse
        {
            EmbedUrl = GenerateGoogleMapsEmbedUrl(centerLat, centerLng, zoomLevel, markers),
            StaticMapUrl = GenerateStaticMapUrl(centerLat, centerLng, zoomLevel, width, height, markers),
            Width = width,
            Height = height,
            ZoomLevel = zoomLevel,
            Markers = markers
        };

        return response;
    }

    public string GenerateGoogleMapsUrl(double originLat, double originLng, double destLat, double destLng)
    {
        return $"https://www.google.com/maps/dir/{originLat},{originLng}/{destLat},{destLng}";
    }

    public string GenerateAppleMapsUrl(double originLat, double originLng, double destLat, double destLng)
    {
        return $"https://maps.apple.com/?saddr={originLat},{originLng}&daddr={destLat},{destLng}";
    }

    public async Task<double> CalculateDistanceAsync(
        double lat1, double lng1, 
        double lat2, double lng2,
        CancellationToken cancellationToken = default)
    {
        // Use the Location value object's distance calculation
        var location1 = new Location((decimal)lat1, (decimal)lng1);
        var location2 = new Location((decimal)lat2, (decimal)lng2);
        
        var distanceMeters = location1.DistanceTo(location2);
        return Math.Round(distanceMeters / 1000.0, 2); // Convert to kilometers
    }

    private static void ValidateCoordinates(double lat, double lng)
    {
        if (lat < -90 || lat > 90)
            throw new ArgumentException($"Invalid latitude: {lat}. Must be between -90 and 90.");
        
        if (lng < -180 || lng > 180)
            throw new ArgumentException($"Invalid longitude: {lng}. Must be between -180 and 180.");
    }

    private static int CalculateEstimatedTravelTime(double distanceKm, string travelMode)
    {
        // Simple time estimation based on travel mode
        var speedKmh = travelMode.ToLowerInvariant() switch
        {
            "walking" => 5.0, // 5 km/h walking speed
            "driving" => 40.0, // 40 km/h average city driving
            "cycling" => 15.0, // 15 km/h cycling speed
            "transit" => 25.0, // 25 km/h average transit speed
            _ => 5.0 // Default to walking
        };

        var timeHours = distanceKm / speedKmh;
        return Math.Max(1, (int)Math.Ceiling(timeHours * 60)); // Convert to minutes, minimum 1 minute
    }

    private static List<NavigationStep> GenerateBasicNavigationSteps(
        double originLat, double originLng, 
        double destLat, double destLng, 
        string travelMode)
    {
        var steps = new List<NavigationStep>();

        // Calculate bearing for direction
        var bearing = CalculateBearing(originLat, originLng, destLat, destLng);
        var direction = GetDirectionFromBearing(bearing);
        var distance = CalculateHaversineDistance(originLat, originLng, destLat, destLng);

        // Simple single-step navigation (in a real implementation, this would use actual routing)
        steps.Add(new NavigationStep
        {
            Instruction = $"Head {direction} for {distance:F1} km",
            DistanceMeters = distance * 1000,
            DurationSeconds = CalculateEstimatedTravelTime(distance, travelMode) * 60,
            StartLocation = new LocationInfo { Latitude = (decimal)originLat, Longitude = (decimal)originLng },
            EndLocation = new LocationInfo { Latitude = (decimal)destLat, Longitude = (decimal)destLng },
            TravelMode = travelMode
        });

        return steps;
    }

    private static List<LocationInfo> GenerateSimplePolyline(double originLat, double originLng, double destLat, double destLng)
    {
        // Simple straight-line polyline (in a real implementation, this would follow roads)
        return new List<LocationInfo>
        {
            new() { Latitude = (decimal)originLat, Longitude = (decimal)originLng },
            new() { Latitude = (decimal)destLat, Longitude = (decimal)destLng }
        };
    }

    private static double CalculateBearing(double lat1, double lng1, double lat2, double lng2)
    {
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var lat1Rad = lat1 * Math.PI / 180;
        var lat2Rad = lat2 * Math.PI / 180;

        var y = Math.Sin(dLng) * Math.Cos(lat2Rad);
        var x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) - Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLng);

        var bearing = Math.Atan2(y, x) * 180 / Math.PI;
        return (bearing + 360) % 360; // Normalize to 0-360
    }

    private static string GetDirectionFromBearing(double bearing)
    {
        var directions = new[]
        {
            "north", "northeast", "east", "southeast",
            "south", "southwest", "west", "northwest"
        };

        var index = (int)Math.Round(bearing / 45) % 8;
        return directions[index];
    }

    private static double CalculateHaversineDistance(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371; // Earth's radius in kilometers

        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private string GenerateGoogleMapsEmbedUrl(double centerLat, double centerLng, int zoomLevel, List<MapMarker> markers)
    {
        var apiKey = "YOUR_API_KEY"; // Would be injected from configuration
        var baseUrl = "https://www.google.com/maps/embed/v1/view";
        
        return $"{baseUrl}?key={apiKey}&center={centerLat},{centerLng}&zoom={zoomLevel}";
    }

    private string GenerateStaticMapUrl(double centerLat, double centerLng, int zoomLevel, int width, int height, List<MapMarker> markers)
    {
        var apiKey = "YOUR_API_KEY"; // Would be injected from configuration
        var baseUrl = "https://maps.googleapis.com/maps/api/staticmap";
        
        var markerParams = string.Join("&", markers.Select(m => 
            $"markers=color:{m.Color}|label:{m.Label}|{m.Location.Latitude},{m.Location.Longitude}"));
        
        return $"{baseUrl}?center={centerLat},{centerLng}&zoom={zoomLevel}&size={width}x{height}&{markerParams}&key={apiKey}";
    }
}