using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Chanzup.Infrastructure.Configuration;

namespace Chanzup.Infrastructure.Monitoring.HealthChecks;

public class ExternalServicesHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly ExternalServicesOptions _options;

    public ExternalServicesHealthCheck(HttpClient httpClient, IOptions<ExternalServicesOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, object>();
        var isHealthy = true;
        var exceptions = new List<Exception>();

        // Check Google Maps API
        try
        {
            var mapsResponse = await _httpClient.GetAsync(
                $"{_options.GoogleMaps.BaseUrl}/geocode/json?address=test&key={_options.GoogleMaps.ApiKey}",
                cancellationToken);
            
            results["googleMaps"] = mapsResponse.IsSuccessStatusCode ? "healthy" : "unhealthy";
            if (!mapsResponse.IsSuccessStatusCode)
                isHealthy = false;
        }
        catch (Exception ex)
        {
            results["googleMaps"] = "unhealthy";
            exceptions.Add(ex);
            isHealthy = false;
        }

        // Check SendGrid API (simple validation)
        try
        {
            if (!string.IsNullOrEmpty(_options.SendGrid.ApiKey))
            {
                results["sendGrid"] = "configured";
            }
            else
            {
                results["sendGrid"] = "not_configured";
                isHealthy = false;
            }
        }
        catch (Exception ex)
        {
            results["sendGrid"] = "error";
            exceptions.Add(ex);
            isHealthy = false;
        }

        // Check Stripe configuration
        try
        {
            if (!string.IsNullOrEmpty(_options.Stripe.SecretKey) && 
                !string.IsNullOrEmpty(_options.Stripe.PublishableKey))
            {
                results["stripe"] = "configured";
            }
            else
            {
                results["stripe"] = "not_configured";
                isHealthy = false;
            }
        }
        catch (Exception ex)
        {
            results["stripe"] = "error";
            exceptions.Add(ex);
            isHealthy = false;
        }

        results["timestamp"] = DateTime.UtcNow;

        if (isHealthy)
        {
            return HealthCheckResult.Healthy("External services are healthy", results);
        }
        else
        {
            var aggregateException = exceptions.Count > 0 ? new AggregateException(exceptions) : null;
            return HealthCheckResult.Degraded("Some external services are unhealthy", aggregateException, results);
        }
    }
}