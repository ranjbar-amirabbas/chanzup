namespace Chanzup.API.Configuration;

public class ExternalServicesOptions
{
    public const string SectionName = "ExternalServices";
    
    public GoogleMapsOptions GoogleMaps { get; set; } = new();
    public StripeOptions Stripe { get; set; } = new();
    public SendGridOptions SendGrid { get; set; } = new();
    public AzureOptions Azure { get; set; } = new();
}

public class GoogleMapsOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
}

public class StripeOptions
{
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}

public class SendGridOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}

public class AzureOptions
{
    public string StorageConnectionString { get; set; } = string.Empty;
    public string KeyVaultUrl { get; set; } = string.Empty;
}