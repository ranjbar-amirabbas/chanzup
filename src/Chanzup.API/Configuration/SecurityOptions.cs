namespace Chanzup.API.Configuration;

public class SecurityOptions
{
    public const string SectionName = "Security";
    
    public bool RequireHttps { get; set; } = true;
    public bool EnableRateLimiting { get; set; } = true;
    public int MaxRequestsPerMinute { get; set; } = 100;
    public bool EnableLocationVerification { get; set; } = true;
    public int LocationToleranceMeters { get; set; } = 100;
    public bool EnableAntiFraud { get; set; } = true;
    public int SessionTimeoutMinutes { get; set; } = 30;
}

public class GameSettingsOptions
{
    public const string SectionName = "GameSettings";
    
    public int MaxSpinsPerDay { get; set; } = 10;
    public int MaxTokensPerDay { get; set; } = 100;
    public int QRCodeCooldownSeconds { get; set; } = 30;
    public int PrizeExpirationDays { get; set; } = 30;
    public int DefaultTokensPerScan { get; set; } = 10;
}

public class HealthChecksOptions
{
    public const string SectionName = "HealthChecks";
    
    public bool Enabled { get; set; } = true;
    public int DatabaseTimeoutSeconds { get; set; } = 30;
    public int RedisTimeoutSeconds { get; set; } = 10;
    public int ExternalServiceTimeoutSeconds { get; set; } = 15;
}

public class CachingOptions
{
    public const string SectionName = "Caching";
    
    public int DefaultExpirationMinutes { get; set; } = 60;
    public int SessionExpirationMinutes { get; set; } = 30;
    public int AnalyticsExpirationMinutes { get; set; } = 15;
}