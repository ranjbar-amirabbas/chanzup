using System.ComponentModel.DataAnnotations;
using Chanzup.Domain.Entities;

namespace Chanzup.Application.DTOs;

public class CreateCampaignRequest
{
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public GameType GameType { get; set; } = GameType.WheelOfLuck;

    [Range(1, 100)]
    public int TokenCostPerSpin { get; set; } = 1;

    [Range(1, 50)]
    public int MaxSpinsPerDay { get; set; } = 10;

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public List<CreatePrizeRequest> Prizes { get; set; } = new();
}

public class CreatePrizeRequest
{
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Value { get; set; }

    [Range(1, int.MaxValue)]
    public int TotalQuantity { get; set; }

    [Range(0.0001, 1.0)]
    public decimal WinProbability { get; set; }
}