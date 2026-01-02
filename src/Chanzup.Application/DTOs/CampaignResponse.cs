using Chanzup.Domain.Entities;

namespace Chanzup.Application.DTOs;

public class CampaignResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GameType GameType { get; set; }
    public int TokenCostPerSpin { get; set; }
    public int MaxSpinsPerDay { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<PrizeResponse> Prizes { get; set; } = new();
    public string? QRCodeUrl { get; set; }
    public BusinessInfo? Business { get; set; }
}

public class BusinessInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public double? DistanceKm { get; set; }
}

public class PrizeResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Value { get; set; }
    public int TotalQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public decimal WinProbability { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}