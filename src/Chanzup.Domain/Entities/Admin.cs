using Chanzup.Domain.ValueObjects;

namespace Chanzup.Domain.Entities;

public class Admin
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Email Email { get; set; } = new("placeholder@example.com");
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public AdminRole Role { get; set; } = AdminRole.Moderator;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }

    // Navigation properties
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<BusinessApplication> ReviewedApplications { get; set; } = new List<BusinessApplication>();
    public ICollection<DisputeResolution> DisputeResolutions { get; set; } = new List<DisputeResolution>();

    // Domain methods
    public string GetFullName()
    {
        return $"{FirstName} {LastName}".Trim();
    }

    public bool CanManageUsers()
    {
        return IsActive && (Role == AdminRole.SuperAdmin || Role == AdminRole.Admin);
    }

    public bool CanManageBusinesses()
    {
        return IsActive && Role != AdminRole.ReadOnly;
    }

    public bool CanViewSystemLogs()
    {
        return IsActive;
    }

    public bool CanModifySystemSettings()
    {
        return IsActive && (Role == AdminRole.SuperAdmin || Role == AdminRole.Admin);
    }

    public bool CanResolveDisputes()
    {
        return IsActive && Role != AdminRole.ReadOnly;
    }

    public bool CanSuspendAccounts()
    {
        return IsActive && Role != AdminRole.ReadOnly;
    }

    public bool CanApproveBusinesses()
    {
        return IsActive && Role != AdminRole.ReadOnly;
    }
}

public enum AdminRole
{
    ReadOnly = 0,
    Moderator = 1,
    Admin = 2,
    SuperAdmin = 3
}