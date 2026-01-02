using Microsoft.AspNetCore.Authorization;

namespace Chanzup.API.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

public class TenantRequirement : IAuthorizationRequirement
{
}