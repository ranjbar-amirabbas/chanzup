using System.ComponentModel.DataAnnotations;

namespace Chanzup.Application.DTOs;

public class QRScanRequest
{
    [Required]
    public string QRCode { get; set; } = string.Empty;

    [Required]
    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Required]
    [Range(-180, 180)]
    public decimal Longitude { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}