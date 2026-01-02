using System.ComponentModel.DataAnnotations;
using Chanzup.Domain.Entities;

namespace Chanzup.Application.DTOs;

public class UpdateCampaignRequest
{
    [StringLength(255, MinimumLength = 1)]
    public string? Name { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(1, 100)]
    public int? TokenCostPerSpin { get; set; }

    [Range(1, 50)]
    public int? MaxSpinsPerDay { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool? IsActive { get; set; }
}