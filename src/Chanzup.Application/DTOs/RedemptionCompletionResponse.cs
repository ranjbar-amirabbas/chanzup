namespace Chanzup.Application.DTOs;

public class RedemptionCompletionResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? RedemptionId { get; set; }
    public DateTime? RedeemedAt { get; set; }
    public PlayerPrizeResponse? Prize { get; set; }
}