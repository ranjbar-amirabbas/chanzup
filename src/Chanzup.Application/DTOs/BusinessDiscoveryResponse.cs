namespace Chanzup.Application.DTOs;

public class BusinessDiscoveryResponse
{
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public LocationInfo Location { get; set; } = new();
    public double DistanceKm { get; set; }
    public int ActiveCampaigns { get; set; }
    public List<CampaignSummary> Campaigns { get; set; } = new();
    public string? Category { get; set; }
    public bool IsOpen { get; set; } = true;
    public BusinessHours? Hours { get; set; }
}

public class LocationInfo
{
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}

public class CampaignSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string GameType { get; set; } = string.Empty;
    public int TokenCostPerSpin { get; set; }
    public int MaxSpinsPerDay { get; set; }
    public List<PrizeSummary> TopPrizes { get; set; } = new();
    public int AvailablePrizes { get; set; }
    public decimal? TotalPrizeValue { get; set; }
    public bool HasSpecialPromotion { get; set; }
}

public class PrizeSummary
{
    public string Name { get; set; } = string.Empty;
    public decimal? Value { get; set; }
    public int RemainingQuantity { get; set; }
    public bool IsSpecialPromotion { get; set; }
}

public class BusinessHours
{
    public string Monday { get; set; } = "9:00 AM - 5:00 PM";
    public string Tuesday { get; set; } = "9:00 AM - 5:00 PM";
    public string Wednesday { get; set; } = "9:00 AM - 5:00 PM";
    public string Thursday { get; set; } = "9:00 AM - 5:00 PM";
    public string Friday { get; set; } = "9:00 AM - 5:00 PM";
    public string Saturday { get; set; } = "10:00 AM - 4:00 PM";
    public string Sunday { get; set; } = "Closed";
}

public class NearbyBusinessesResponse
{
    public LocationInfo SearchLocation { get; set; } = new();
    public double RadiusKm { get; set; }
    public List<BusinessDiscoveryResponse> Businesses { get; set; } = new();
    public int TotalBusinesses { get; set; }
    public int TotalCampaigns { get; set; }
    public string? Category { get; set; }
    public bool ActiveOnly { get; set; } = true;
}