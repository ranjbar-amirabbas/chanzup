namespace Chanzup.Domain.Entities;

public class AnalyticsEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public Guid? PlayerId { get; set; }
    public Guid? CampaignId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty; // JSON data
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Business Business { get; set; } = null!;
    public Player? Player { get; set; }
    public Campaign? Campaign { get; set; }

    // Domain methods
    public static AnalyticsEvent CreateQRScanEvent(Guid businessId, Guid playerId, Guid campaignId, int tokensEarned)
    {
        return new AnalyticsEvent
        {
            BusinessId = businessId,
            PlayerId = playerId,
            CampaignId = campaignId,
            EventType = AnalyticsEventType.QRScan,
            EventData = System.Text.Json.JsonSerializer.Serialize(new { tokensEarned })
        };
    }

    public static AnalyticsEvent CreateWheelSpinEvent(Guid businessId, Guid playerId, Guid campaignId, int tokensSpent, bool wonPrize, Guid? prizeId = null)
    {
        return new AnalyticsEvent
        {
            BusinessId = businessId,
            PlayerId = playerId,
            CampaignId = campaignId,
            EventType = AnalyticsEventType.WheelSpin,
            EventData = System.Text.Json.JsonSerializer.Serialize(new { tokensSpent, wonPrize, prizeId })
        };
    }

    public static AnalyticsEvent CreatePrizeRedemptionEvent(Guid businessId, Guid playerId, Guid prizeId)
    {
        return new AnalyticsEvent
        {
            BusinessId = businessId,
            PlayerId = playerId,
            EventType = AnalyticsEventType.PrizeRedemption,
            EventData = System.Text.Json.JsonSerializer.Serialize(new { prizeId })
        };
    }

    public static AnalyticsEvent CreatePlayerRegistrationEvent(Guid playerId)
    {
        return new AnalyticsEvent
        {
            BusinessId = Guid.Empty, // System-wide event
            PlayerId = playerId,
            EventType = AnalyticsEventType.PlayerRegistration,
            EventData = "{}"
        };
    }

    public static AnalyticsEvent CreateCampaignCreatedEvent(Guid businessId, Guid campaignId)
    {
        return new AnalyticsEvent
        {
            BusinessId = businessId,
            CampaignId = campaignId,
            EventType = AnalyticsEventType.CampaignCreated,
            EventData = "{}"
        };
    }
}

public static class AnalyticsEventType
{
    public const string QRScan = "qr_scan";
    public const string WheelSpin = "wheel_spin";
    public const string PrizeRedemption = "prize_redemption";
    public const string PlayerRegistration = "player_registration";
    public const string CampaignCreated = "campaign_created";
    public const string TokenPurchase = "token_purchase";
    public const string BusinessRegistration = "business_registration";
}