using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Chanzup.API.Authorization;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Check if user has the required permission
        var permissions = context.User.FindAll("permission").Select(c => c.Value);
        
        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public class TenantHandler : AuthorizationHandler<TenantRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantRequirement requirement)
    {
        // Check if user has a tenant ID (for business users)
        var tenantId = context.User.FindFirst("tenantId")?.Value;
        
        if (!string.IsNullOrEmpty(tenantId) && Guid.TryParse(tenantId, out _))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}