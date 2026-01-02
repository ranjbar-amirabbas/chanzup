namespace Chanzup.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public Guid? UserId { get; set; }
    public string UserType { get; set; } = string.Empty; // Admin, Staff, Player
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? AdditionalData { get; set; }

    // Navigation properties
    public Admin? Admin { get; set; }

    // Domain methods
    public static AuditLog Create(
        string action,
        string entityType,
        Guid? entityId,
        Guid? userId,
        string userType,
        string? userEmail = null,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? additionalData = null)
    {
        return new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            UserId = userId,
            UserType = userType,
            UserEmail = userEmail,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            AdditionalData = additionalData
        };
    }

    public bool IsSecurityEvent()
    {
        var securityActions = new[]
        {
            "LOGIN_FAILED",
            "ACCOUNT_SUSPENDED",
            "ACCOUNT_BANNED",
            "PASSWORD_CHANGED",
            "PERMISSION_DENIED",
            "SUSPICIOUS_ACTIVITY"
        };

        return securityActions.Contains(Action.ToUpper());
    }

    public bool IsAdminAction()
    {
        return UserType.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }
}