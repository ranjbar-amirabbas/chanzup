using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;

namespace Chanzup.API.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _permission;

    public RequirePermissionAttribute(string permission)
    {
        _permission = permission;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Check if user has the required permission
        var permissions = context.HttpContext.User.FindAll("permission").Select(c => c.Value);
        
        if (!permissions.Contains(_permission))
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public RequireRoleAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Check if user has any of the required roles
        var userRole = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (string.IsNullOrEmpty(userRole) || !_roles.Contains(userRole))
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireTenantAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Check if user has tenant ID (business users only)
        var userRole = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;
        var tenantId = context.HttpContext.User.FindFirst("tenantId")?.Value;
        
        if ((userRole == "BusinessOwner" || userRole == "Staff") && string.IsNullOrEmpty(tenantId))
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireSubscriptionTierAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly SubscriptionTier _minimumTier;

    public RequireSubscriptionTierAttribute(SubscriptionTier minimumTier)
    {
        _minimumTier = minimumTier;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Get user role and tenant ID
        var userRole = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;
        var tenantIdClaim = context.HttpContext.User.FindFirst("tenantId")?.Value;

        // Only business users have subscription tiers
        if (userRole != "BusinessOwner" && userRole != "Staff")
        {
            context.Result = new ForbidResult();
            return;
        }

        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            context.Result = new ForbidResult();
            return;
        }

        // Get business subscription tier from database
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();
        var business = await dbContext.Businesses.FindAsync(tenantId);

        if (business == null || !business.IsActive)
        {
            context.Result = new ForbidResult();
            return;
        }

        // Check if business subscription tier meets minimum requirement
        if (business.SubscriptionTier < _minimumTier)
        {
            context.Result = new ObjectResult(new
            {
                error = "Premium subscription required",
                message = $"This feature requires {_minimumTier} subscription or higher. Current subscription: {business.SubscriptionTier}",
                requiredTier = _minimumTier.ToString(),
                currentTier = business.SubscriptionTier.ToString()
            })
            {
                StatusCode = 402 // Payment Required
            };
            return;
        }
    }
}