using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;

namespace Chanzup.API.Controllers;

/// <summary>
/// Health check endpoints for monitoring system status
/// </summary>
/// <remarks>
/// Provides various health check endpoints for monitoring the application's health status,
/// including database connectivity, external services, and overall system readiness.
/// </remarks>
[ApiController]
[Route("[controller]")]
[AllowAnonymous]
[Produces("application/json")]
[SwaggerTag("System health monitoring and status endpoints")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    /// <summary>
    /// Comprehensive health check for all system components
    /// </summary>
    /// <returns>Detailed health status of all registered health checks</returns>
    /// <response code="200">System is healthy or degraded</response>
    /// <response code="503">System is unhealthy</response>
    /// <remarks>
    /// This endpoint performs a comprehensive health check of all registered components including:
    /// - Database connectivity
    /// - External service availability
    /// - Cache systems (Redis)
    /// - File system access
    /// 
    /// **Status Levels:**
    /// - **Healthy**: All components are functioning normally
    /// - **Degraded**: Some non-critical components have issues
    /// - **Unhealthy**: Critical components are failing
    /// </remarks>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get comprehensive system health status",
        Description = "Performs health checks on all registered components and returns detailed status information.",
        OperationId = "GetHealthStatus",
        Tags = new[] { "❤️ Health Checks" }
    )]
    [SwaggerResponse(200, "System health status", typeof(object))]
    [SwaggerResponse(503, "System is unhealthy", typeof(object))]
    public async Task<IActionResult> Get()
    {
        var report = await _healthCheckService.CheckHealthAsync();
        
        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            entries = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                data = e.Value.Data,
                exception = e.Value.Exception?.Message
            })
        };

        var statusCode = report.Status switch
        {
            HealthStatus.Healthy => 200,
            HealthStatus.Degraded => 200,
            HealthStatus.Unhealthy => 503,
            _ => 500
        };

        return StatusCode(statusCode, response);
    }

    /// <summary>
    /// Readiness probe for deployment and load balancer health checks
    /// </summary>
    /// <returns>Simple ready/not ready status</returns>
    /// <response code="200">Application is ready to serve traffic</response>
    /// <response code="503">Application is not ready</response>
    /// <remarks>
    /// This endpoint is designed for Kubernetes readiness probes and load balancer health checks.
    /// It only checks components tagged with "ready" which are essential for serving traffic.
    /// 
    /// **Use Cases:**
    /// - Kubernetes readiness probes
    /// - Load balancer health checks
    /// - Deployment verification
    /// </remarks>
    [HttpGet("ready")]
    [SwaggerOperation(
        Summary = "Check if application is ready to serve traffic",
        Description = "Lightweight health check for readiness probes. Only checks essential components needed to serve requests.",
        OperationId = "GetReadinessStatus",
        Tags = new[] { "❤️ Health Checks" }
    )]
    [SwaggerResponse(200, "Application is ready", typeof(string))]
    [SwaggerResponse(503, "Application is not ready", typeof(string))]
    public async Task<IActionResult> Ready()
    {
        var report = await _healthCheckService.CheckHealthAsync(predicate: check => 
            check.Tags.Contains("ready"));
        
        return report.Status == HealthStatus.Healthy ? Ok("Ready") : StatusCode(503, "Not Ready");
    }

    /// <summary>
    /// Liveness probe to verify the application is running
    /// </summary>
    /// <returns>Simple alive status with timestamp</returns>
    /// <response code="200">Application is alive and responding</response>
    /// <remarks>
    /// This endpoint is designed for Kubernetes liveness probes and basic uptime monitoring.
    /// It performs minimal checks and should always return 200 if the application is running.
    /// 
    /// **Use Cases:**
    /// - Kubernetes liveness probes
    /// - Basic uptime monitoring
    /// - Application restart triggers
    /// </remarks>
    [HttpGet("live")]
    [SwaggerOperation(
        Summary = "Check if application is alive and responding",
        Description = "Minimal health check for liveness probes. Always returns 200 if the application is running.",
        OperationId = "GetLivenessStatus",
        Tags = new[] { "❤️ Health Checks" }
    )]
    [SwaggerResponse(200, "Application is alive", typeof(object))]
    public IActionResult Live()
    {
        return Ok(new { 
            status = "alive", 
            timestamp = DateTime.UtcNow,
            uptime = Environment.TickCount64,
            version = "1.0.0"
        });
    }
}