namespace Chanzup.API.Configuration;

public class ExternalServicesOptions
{
    public const string SectionName = "ExternalServices";
    
    public string? PaymentGatewayUrl { get; set; }
    public string? PaymentGatewayApiKey { get; set; }
    public string? EmailServiceUrl { get; set; }
    public string? EmailServiceApiKey { get; set; }
    public string? SmsServiceUrl { get; set; }
    public string? SmsServiceApiKey { get; set; }
}

public class SecurityOptions
{
    public const string SectionName = "Security";
    
    public int MaxLoginAttempts { get; set; } = 5;
    public int LoginLockoutMinutes { get; set; } = 15;
    public bool RequireHttps { get; set; } = true;
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}

public class GameSettingsOptions
{
    public const string SectionName = "GameSettings";
    
    public int DefaultTokensPerSpin { get; set; } = 5;
    public int MaxSpinsPerDay { get; set; } = 10;
    public decimal DefaultTokenPrice { get; set; } = 0.10m;
    public int FreeTokensOnRegistration { get; set; } = 25;
}

public class HealthChecksOptions
{
    public const string SectionName = "HealthChecks";
    
    public bool EnableDetailedErrors { get; set; } = false;
    public int TimeoutSeconds { get; set; } = 30;
}

public class CachingOptions
{
    public const string SectionName = "Caching";
    
    public bool EnableRedis { get; set; } = false;
    public string? RedisConnectionString { get; set; }
    public int DefaultCacheExpirationMinutes { get; set; } = 60;
}