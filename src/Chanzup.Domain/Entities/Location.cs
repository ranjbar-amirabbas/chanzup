namespace Chanzup.Domain.Entities;

public class BusinessLocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public ValueObjects.Location Location { get; set; } = new(0, 0);
    public string QRCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Business Business { get; set; } = null!;

    // Domain methods
    public void UpdateLocation(decimal latitude, decimal longitude)
    {
        Location = new ValueObjects.Location(latitude, longitude);
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsWithinRadius(ValueObjects.Location playerLocation, double radiusMeters = 100)
    {
        return Location.IsWithinRadius(playerLocation, radiusMeters);
    }
}