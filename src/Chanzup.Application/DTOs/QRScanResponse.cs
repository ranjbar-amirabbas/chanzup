namespace Chanzup.Application.DTOs;

public class QRScanResponse
{
    public Guid SessionId { get; set; }
    public int TokensEarned { get; set; }
    public int NewBalance { get; set; }
    public bool CanSpin { get; set; }
    public CampaignInfo? Campaign { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CampaignInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TokenCostPerSpin { get; set; }
    public int RemainingSpinsToday { get; set; }
}