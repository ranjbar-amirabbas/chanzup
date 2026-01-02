using System.ComponentModel.DataAnnotations;

namespace Chanzup.Application.DTOs;

public class RedemptionCompletionRequest
{
    [Required]
    [StringLength(20, MinimumLength = 6)]
    public string RedemptionCode { get; set; } = string.Empty;
    
    [Required]
    public Guid StaffId { get; set; }
}