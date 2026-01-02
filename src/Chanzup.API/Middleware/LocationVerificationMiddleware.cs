using System.Text.Json;
using Chanzup.Application.Interfaces;

namespace Chanzup.API.Middleware;

public class LocationVerificationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LocationVerificationMiddleware> _logger;

    public LocationVerificationMiddleware(RequestDelegate next, ILogger<LocationVerificationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISecurityService securityService)
    {
        // Only apply location verification to specific endpoints
        if (!RequiresLocationVerification(context.Request.Path))
        {
            await _next(context);
            return;
        }

        try
        {
            // Extract location data from request
            var locationData = await ExtractLocationDataAsync(context);
            
            if (locationData == null)
            {
                await HandleMissingLocationAsync(context);
                return;
            }

            // Verify location against IP address
            var ipAddress = GetClientIpAddress(context);
            var isLocationValid = await securityService.VerifyLocationAsync(
                ipAddress, 
                locationData.Latitude, 
                locationData.Longitude, 
                locationData.ToleranceMeters);

            if (!isLocationValid)
            {
                await HandleInvalidLocationAsync(context, locationData, ipAddress);
                return;
            }

            // Location is valid, continue processing
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in location verification middleware");
            await HandleLocationVerificationErrorAsync(context);
        }
    }

    private bool RequiresLocationVerification(string path)
    {
        var locationRequiredEndpoints = new[]
        {
            "/api/qr/scan",
            "/api/discovery/nearby"
        };

        return locationRequiredEndpoints.Any(endpoint => 
            path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<LocationData?> ExtractLocationDataAsync(HttpContext context)
    {
        try
        {
            // For QR scan requests, location might be in the request body
            if (context.Request.Path.StartsWithSegments("/api/qr/scan"))
            {
                context.Request.EnableBuffering();
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (!string.IsNullOrEmpty(body))
                {
                    var requestData = JsonSerializer.Deserialize<JsonElement>(body);
                    
                    if (requestData.TryGetProperty("location", out var locationElement))
                    {
                        if (locationElement.TryGetProperty("latitude", out var latElement) &&
                            locationElement.TryGetProperty("longitude", out var lngElement))
                        {
                            return new LocationData
                            {
                                Latitude = latElement.GetDecimal(),
                                Longitude = lngElement.GetDecimal(),
                                ToleranceMeters = 100 // Default tolerance
                            };
                        }
                    }
                }
            }

            // For discovery requests, location might be in query parameters
            if (context.Request.Path.StartsWithSegments("/api/discovery/nearby"))
            {
                if (context.Request.Query.TryGetValue("lat", out var latValue) &&
                    context.Request.Query.TryGetValue("lng", out var lngValue))
                {
                    if (decimal.TryParse(latValue, out var lat) && decimal.TryParse(lngValue, out var lng))
                    {
                        return new LocationData
                        {
                            Latitude = lat,
                            Longitude = lng,
                            ToleranceMeters = 1000 // Larger tolerance for discovery
                        };
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting location data from request");
            return null;
        }
    }

    private async Task HandleMissingLocationAsync(HttpContext context)
    {
        _logger.LogWarning("Location verification required but no location data provided for {Path}", context.Request.Path);
        
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            error = "Location required",
            message = "This operation requires location data for verification"
        };
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private async Task HandleInvalidLocationAsync(HttpContext context, LocationData locationData, string ipAddress)
    {
        _logger.LogWarning("Location verification failed for IP {IpAddress}. Provided: {Lat}, {Lng}", 
            ipAddress, locationData.Latitude, locationData.Longitude);
        
        context.Response.StatusCode = 403;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            error = "Location verification failed",
            message = "Your location could not be verified. Please ensure you are at the correct location."
        };
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private async Task HandleLocationVerificationErrorAsync(HttpContext context)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            error = "Location verification error",
            message = "Unable to verify location at this time"
        };
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private string GetClientIpAddress(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }
        }

        if (context.Request.Headers.ContainsKey("X-Real-IP"))
        {
            var realIp = context.Request.Headers["X-Real-IP"].ToString();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }
        }

        return context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";
    }
}

public class LocationData
{
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal ToleranceMeters { get; set; } = 100;
}