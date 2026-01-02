using Chanzup.Domain.ValueObjects;

namespace Chanzup.Domain.Entities;

public class Business
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Email Email { get; set; } = new("placeholder@example.com");
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public Location? Location { get; set; }
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Basic;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
    public ICollection<BusinessLocation> BusinessLocations { get; set; } = new List<BusinessLocation>();
    public ICollection<Staff> Staff { get; set; } = new List<Staff>();

    // Domain methods
    public bool CanCreateCampaign()
    {
        return IsActive && SubscriptionTier != SubscriptionTier.Suspended;
    }

    public int GetMaxCampaigns()
    {
        return SubscriptionTier switch
        {
            SubscriptionTier.Basic => 1,
            SubscriptionTier.Premium => 5,
            SubscriptionTier.Enterprise => int.MaxValue,
            _ => 0
        };
    }

    public void UpdateLocation(decimal latitude, decimal longitude)
    {
        Location = new Location(latitude, longitude);
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum SubscriptionTier
{
    Suspended = -1,
    Basic = 0,
    Premium = 1,
    Enterprise = 2
}