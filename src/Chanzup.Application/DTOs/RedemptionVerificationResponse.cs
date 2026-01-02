namespace Chanzup.Application.DTOs;

public class RedemptionVerificationResponse
{
    public bool IsValid { get; set; }
    public bool CanRedeem { get; set; }
    public string? ErrorMessage { get; set; }
    public PlayerPrizeResponse? Prize { get; set; }
}