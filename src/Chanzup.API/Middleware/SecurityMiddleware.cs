using System.Net;
using System.Text.Json;
using Chanzup.Application.Interfaces;

namespace Chanzup.API.Middleware;

public class SecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityMiddleware> _logger;

    public SecurityMiddleware(RequestDelegate next, ILogger<SecurityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISecurityService securityService, IRateLimitingService rateLimitingService)
    {
        try
        {
            // Add security headers
            await AddSecurityHeadersAsync(context, securityService);

            // Check if IP is blocked
            var ipAddress = GetClientIpAddress(context);
            if (await securityService.IsIpAddressBlockedAsync(ipAddress))
            {
                await HandleBlockedIpAsync(context, ipAddress);
                return;
            }

            // Apply rate limiting
            if (!await ApplyRateLimitingAsync(context, rateLimitingService, ipAddress))
            {
                return;
            }

            // Check for suspicious location (for location-based endpoints)
            if (IsLocationBasedEndpoint(context.Request.Path))
            {
                if (await securityService.IsLocationSuspiciousAsync(ipAddress))
                {
                    await securityService.LogSecurityEventAsync("SUSPICIOUS_LOCATION_ACCESS", null, null, ipAddress, 
                        context.Request.Headers.UserAgent, new { Path = context.Request.Path });
                }
            }

            // Continue to next middleware
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in security middleware");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task AddSecurityHeadersAsync(HttpContext context, ISecurityService securityService)
    {
        var headers = await securityService.GetSecurityHeadersAsync();
        
        foreach (var header in headers)
        {
            if (!context.Response.Headers.ContainsKey(header.Key))
            {
                context.Response.Headers.Add(header.Key, header.Value);
            }
        }
    }

    private async Task<bool> ApplyRateLimitingAsync(HttpContext context, IRateLimitingService rateLimitingService, string ipAddress)
    {
        var endpoint = context.Request.Path.Value?.ToLower();
        var rateLimitConfig = GetRateLimitConfig(endpoint);

        if (rateLimitConfig != null)
        {
            var key = $"{ipAddress}:{endpoint}";
            
            if (!await rateLimitingService.IsWithinRateLimitAsync(key, rateLimitConfig.MaxRequests, rateLimitConfig.TimeWindow))
            {
                await HandleRateLimitExceededAsync(context, rateLimitConfig);
                return false;
            }

            await rateLimitingService.RecordRequestAsync(key);
        }

        return true;
    }

    private async Task HandleBlockedIpAsync(HttpContext context, string ipAddress)
    {
        _logger.LogWarning("Blocked IP {IpAddress} attempted to access {Path}", ipAddress, context.Request.Path);
        
        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            error = "Access denied",
            message = "Your IP address has been blocked due to suspicious activity"
        };
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private async Task HandleRateLimitExceededAsync(HttpContext context, RateLimitConfig config)
    {
        _logger.LogWarning("Rate limit exceeded for IP {IpAddress} on endpoint {Path}", 
            GetClientIpAddress(context), context.Request.Path);
        
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";
        
        // Add rate limit headers
        context.Response.Headers.Add("X-RateLimit-Limit", config.MaxRequests.ToString());
        context.Response.Headers.Add("X-RateLimit-Window", config.TimeWindow.TotalSeconds.ToString());
        context.Response.Headers.Add("Retry-After", config.TimeWindow.TotalSeconds.ToString());
        
        var response = new
        {
            error = "Rate limit exceeded",
            message = $"Too many requests. Limit: {config.MaxRequests} per {config.TimeWindow.TotalMinutes} minutes"
        };
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            error = "Internal server error",
            message = "An unexpected error occurred"
        };
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP first (for load balancers/proxies)
        if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // Take the first IP in the chain
                return forwardedFor.Split(',')[0].Trim();
            }
        }

        // Check for real IP header
        if (context.Request.Headers.ContainsKey("X-Real-IP"))
        {
            var realIp = context.Request.Headers["X-Real-IP"].ToString();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }
        }

        // Fall back to connection remote IP
        return context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";
    }

    private bool IsLocationBasedEndpoint(string path)
    {
        var locationEndpoints = new[]
        {
            "/api/qr/scan",
            "/api/discovery/nearby",
            "/api/player/nearby"
        };

        return locationEndpoints.Any(endpoint => path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase));
    }

    private RateLimitConfig? GetRateLimitConfig(string? endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
            return null;

        // Define rate limits for different endpoints
        var rateLimits = new Dictionary<string, RateLimitConfig>
        {
            { "/api/auth/login", new RateLimitConfig(5, TimeSpan.FromMinutes(1)) },
            { "/api/auth/register", new RateLimitConfig(3, TimeSpan.FromMinutes(1)) },
            { "/api/qr/scan", new RateLimitConfig(1, TimeSpan.FromSeconds(30)) },
            { "/api/wheel/spin", new RateLimitConfig(10, TimeSpan.FromMinutes(1)) },
            { "/api/campaigns", new RateLimitConfig(20, TimeSpan.FromMinutes(1)) },
            { "/api/analytics", new RateLimitConfig(30, TimeSpan.FromMinutes(1)) }
        };

        // Find matching rate limit config
        foreach (var config in rateLimits)
        {
            if (endpoint.StartsWith(config.Key, StringComparison.OrdinalIgnoreCase))
            {
                return config.Value;
            }
        }

        // Default rate limit for all other endpoints
        return new RateLimitConfig(100, TimeSpan.FromMinutes(1));
    }
}

public class RateLimitConfig
{
    public int MaxRequests { get; }
    public TimeSpan TimeWindow { get; }

    public RateLimitConfig(int maxRequests, TimeSpan timeWindow)
    {
        MaxRequests = maxRequests;
        TimeWindow = timeWindow;
    }
}