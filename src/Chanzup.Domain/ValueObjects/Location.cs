namespace Chanzup.Domain.ValueObjects;

public record Location
{
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }

    public Location(decimal latitude, decimal longitude)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentException("Latitude must be between -90 and 90 degrees", nameof(latitude));
        
        if (longitude < -180 || longitude > 180)
            throw new ArgumentException("Longitude must be between -180 and 180 degrees", nameof(longitude));

        Latitude = latitude;
        Longitude = longitude;
    }

    public double DistanceTo(Location other)
    {
        const double earthRadiusKm = 6371.0;
        
        var lat1Rad = (double)Latitude * Math.PI / 180;
        var lat2Rad = (double)other.Latitude * Math.PI / 180;
        var deltaLatRad = (double)(other.Latitude - Latitude) * Math.PI / 180;
        var deltaLonRad = (double)(other.Longitude - Longitude) * Math.PI / 180;

        var a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return earthRadiusKm * c * 1000; // Return distance in meters
    }

    public bool IsWithinRadius(Location other, double radiusMeters)
    {
        return DistanceTo(other) <= radiusMeters;
    }
}