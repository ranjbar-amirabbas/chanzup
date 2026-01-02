namespace Chanzup.Application.DTOs;

public class NavigationResponse
{
    public LocationInfo Origin { get; set; } = new();
    public LocationInfo Destination { get; set; } = new();
    public NavigationInfo Navigation { get; set; } = new();
    public BusinessInfo Business { get; set; } = new();
}

public class NavigationInfo
{
    public double DistanceKm { get; set; }
    public int EstimatedTravelTimeMinutes { get; set; }
    public string TravelMode { get; set; } = "walking"; // walking, driving, transit
    public List<NavigationStep> Steps { get; set; } = new();
    public string GoogleMapsUrl { get; set; } = string.Empty;
    public string AppleMapsUrl { get; set; } = string.Empty;
    public RouteInfo Route { get; set; } = new();
}

public class NavigationStep
{
    public string Instruction { get; set; } = string.Empty;
    public double DistanceMeters { get; set; }
    public int DurationSeconds { get; set; }
    public LocationInfo StartLocation { get; set; } = new();
    public LocationInfo EndLocation { get; set; } = new();
    public string TravelMode { get; set; } = "walking";
}

public class RouteInfo
{
    public List<LocationInfo> Polyline { get; set; } = new();
    public string EncodedPolyline { get; set; } = string.Empty;
    public RouteBounds Bounds { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class RouteBounds
{
    public LocationInfo Northeast { get; set; } = new();
    public LocationInfo Southwest { get; set; } = new();
}

public class MapEmbedResponse
{
    public string EmbedUrl { get; set; } = string.Empty;
    public string StaticMapUrl { get; set; } = string.Empty;
    public int Width { get; set; } = 400;
    public int Height { get; set; } = 300;
    public int ZoomLevel { get; set; } = 15;
    public List<MapMarker> Markers { get; set; } = new();
}

public class MapMarker
{
    public LocationInfo Location { get; set; } = new();
    public string Label { get; set; } = string.Empty;
    public string Color { get; set; } = "red";
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}