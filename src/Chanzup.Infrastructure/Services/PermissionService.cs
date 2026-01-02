using Chanzup.Domain.Entities;

namespace Chanzup.Infrastructure.Services;

public interface IPermissionService
{
    List<string> GetPermissionsForRole(string role, StaffRole? staffRole = null);
}

public class PermissionService : IPermissionService
{
    public List<string> GetPermissionsForRole(string role, StaffRole? staffRole = null)
    {
        var permissions = new List<string>();

        switch (role.ToLower())
        {
            case "admin":
                permissions.AddRange(GetAdminPermissions());
                break;
            case "businessowner":
                permissions.AddRange(GetBusinessOwnerPermissions());
                break;
            case "staff":
                permissions.AddRange(GetStaffPermissions(staffRole ?? StaffRole.Staff));
                break;
            case "player":
                permissions.AddRange(GetPlayerPermissions());
                break;
        }

        return permissions;
    }

    private List<string> GetAdminPermissions()
    {
        return new List<string>
        {
            "admin:read",
            "admin:write",
            "business:read",
            "business:write",
            "business:delete",
            "campaign:read",
            "campaign:write",
            "campaign:delete",
            "analytics:read",
            "analytics:export",
            "user:read",
            "user:write",
            "user:delete",
            "system:configure"
        };
    }

    private List<string> GetBusinessOwnerPermissions()
    {
        return new List<string>
        {
            "business:read",
            "business:write",
            "campaign:read",
            "campaign:write",
            "campaign:delete",
            "analytics:read",
            "analytics:export",
            "staff:read",
            "staff:write",
            "staff:delete",
            "prize:read",
            "prize:write",
            "redemption:verify"
        };
    }

    private List<string> GetStaffPermissions(StaffRole staffRole)
    {
        var permissions = new List<string>
        {
            "campaign:read",
            "redemption:verify",
            "prize:read"
        };

        if (staffRole == StaffRole.Manager)
        {
            permissions.AddRange(new[]
            {
                "campaign:write",
                "analytics:read",
                "staff:read"
            });
        }

        return permissions;
    }

    private List<string> GetPlayerPermissions()
    {
        return new List<string>
        {
            "player:read",
            "player:write",
            "game:play",
            "prize:redeem",
            "wallet:read"
        };
    }
}