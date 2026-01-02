using Chanzup.Domain.ValueObjects;

namespace Chanzup.Domain.Entities;

public class Staff
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public Email Email { get; set; } = new("placeholder@example.com");
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public StaffRole Role { get; set; } = StaffRole.Staff;
    public StaffAccessLevel AccessLevel { get; set; } = StaffAccessLevel.SingleLocation;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Business Business { get; set; } = null!;
    public ICollection<StaffLocationAccess> LocationAccess { get; set; } = new List<StaffLocationAccess>();

    // Domain methods
    public string GetFullName()
    {
        return $"{FirstName} {LastName}".Trim();
    }

    public bool CanManageCampaigns()
    {
        return IsActive && (Role == StaffRole.Manager || Role == StaffRole.Owner);
    }

    public bool CanVerifyRedemptions()
    {
        return IsActive;
    }

    public bool CanViewAnalytics()
    {
        return IsActive && (Role == StaffRole.Manager || Role == StaffRole.Owner);
    }

    public bool HasAccessToLocation(Guid locationId)
    {
        if (!IsActive) return false;
        
        return AccessLevel switch
        {
            StaffAccessLevel.AllLocations => true,
            StaffAccessLevel.SingleLocation => LocationAccess.Any(la => la.LocationId == locationId && la.IsActive),
            StaffAccessLevel.MultipleLocations => LocationAccess.Any(la => la.LocationId == locationId && la.IsActive),
            _ => false
        };
    }

    public bool CanManageCampaignsAtLocation(Guid locationId)
    {
        return CanManageCampaigns() && HasAccessToLocation(locationId);
    }

    public bool CanVerifyRedemptionsAtLocation(Guid locationId)
    {
        return CanVerifyRedemptions() && HasAccessToLocation(locationId);
    }

    public bool CanViewAnalyticsForLocation(Guid locationId)
    {
        return CanViewAnalytics() && HasAccessToLocation(locationId);
    }

    public void GrantAccessToLocation(Guid locationId, LocationPermissions permissions = LocationPermissions.All)
    {
        var existingAccess = LocationAccess.FirstOrDefault(la => la.LocationId == locationId);
        if (existingAccess != null)
        {
            existingAccess.Permissions = permissions;
            existingAccess.IsActive = true;
            existingAccess.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            LocationAccess.Add(new StaffLocationAccess
            {
                StaffId = Id,
                LocationId = locationId,
                Permissions = permissions,
                IsActive = true
            });
        }
        
        // Update access level if needed
        if (AccessLevel == StaffAccessLevel.SingleLocation && LocationAccess.Count(la => la.IsActive) > 1)
        {
            AccessLevel = StaffAccessLevel.MultipleLocations;
        }
        
        UpdatedAt = DateTime.UtcNow;
    }

    public void RevokeAccessToLocation(Guid locationId)
    {
        var existingAccess = LocationAccess.FirstOrDefault(la => la.LocationId == locationId);
        if (existingAccess != null)
        {
            existingAccess.IsActive = false;
            existingAccess.UpdatedAt = DateTime.UtcNow;
        }
        
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAllLocationsAccess()
    {
        AccessLevel = StaffAccessLevel.AllLocations;
        // Clear specific location access when granting all locations access
        foreach (var access in LocationAccess)
        {
            access.IsActive = false;
            access.UpdatedAt = DateTime.UtcNow;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public IEnumerable<Guid> GetAccessibleLocationIds()
    {
        return AccessLevel == StaffAccessLevel.AllLocations
            ? Business.BusinessLocations.Where(bl => bl.IsActive).Select(bl => bl.Id)
            : LocationAccess.Where(la => la.IsActive).Select(la => la.LocationId);
    }

    public bool HasPermissionAtLocation(Guid locationId, LocationPermissions permission)
    {
        if (!HasAccessToLocation(locationId)) return false;
        
        if (AccessLevel == StaffAccessLevel.AllLocations) return true;
        
        var locationAccess = LocationAccess.FirstOrDefault(la => la.LocationId == locationId && la.IsActive);
        return locationAccess?.Permissions.HasFlag(permission) ?? false;
    }
}

public enum StaffRole
{
    Staff = 0,
    Manager = 1,
    Owner = 2
}

public enum StaffAccessLevel
{
    SingleLocation = 0,
    MultipleLocations = 1,
    AllLocations = 2
}

[Flags]
public enum LocationPermissions
{
    None = 0,
    ViewAnalytics = 1,
    ManageCampaigns = 2,
    VerifyRedemptions = 4,
    ManageInventory = 8,
    All = ViewAnalytics | ManageCampaigns | VerifyRedemptions | ManageInventory
}

public class StaffLocationAccess
{
    public Guid StaffId { get; set; }
    public Guid LocationId { get; set; }
    public LocationPermissions Permissions { get; set; } = LocationPermissions.All;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Staff Staff { get; set; } = null!;
    public BusinessLocation Location { get; set; } = null!;
}