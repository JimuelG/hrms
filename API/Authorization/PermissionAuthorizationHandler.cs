using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace API.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; set; } = permission;
}

public sealed class PermissionAuthorizationHandler(
    IPermissionService permissions,
    ITenantContext tenant) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (!tenant.HasTenant) return;

        var userIdClaim = context.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return;

        var userPermissions = await permissions.GetPermissionsAsync(userId, tenant.RequiredTenantId);
        if (userPermissions.Contains(requirement.Permission))
            context.Succeed(requirement);
    }
}