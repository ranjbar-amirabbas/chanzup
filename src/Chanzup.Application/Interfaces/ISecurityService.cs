namespace Chanzup.Application.Interfaces;

public interface ISecurityService
{
    Task<bool> IsIpAddressBlockedAsync(string ipAddress);
    Task<Dictionary<string, string>> GetSecurityHeadersAsync();
    Task<bool> IsLocationSuspiciousAsync(string ipAddress);
    Task LogSecurityEventAsync(string eventType, Guid? userId, Guid? businessId, string? ipAddress, string? userAgent, object? additionalData);
    Task<bool> VerifyLocationAsync(string ipAddress, decimal latitude, decimal longitude, decimal toleranceMeters);
}