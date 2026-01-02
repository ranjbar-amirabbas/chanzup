using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Chanzup.Infrastructure.Monitoring.Metrics;

namespace Chanzup.Infrastructure.Monitoring.Middleware;

public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApplicationMetrics _metrics;

    public MetricsMiddleware(RequestDelegate next, ApplicationMetrics metrics)
    {
        _next = next;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            var endpoint = context.Request.Path.Value ?? "unknown";
            var method = context.Request.Method;
            var statusCode = context.Response.StatusCode;
            var responseTime = stopwatch.Elapsed.TotalSeconds;
            
            _metrics.RecordApiResponseTime(responseTime, endpoint, method, statusCode);
        }
    }
}