using API.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace API.Extensions;
public static class AuthorizationExtensions
{
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorizationBuilder()
            .AddPolicy("Permission:employees.read", p => p.Requirements.Add(new PermissionRequirement("employees.read")))
            .AddPolicy("Permission:employees.write", p => p.Requirements.Add(new PermissionRequirement("employees.write")))
            .AddPolicy("Permission:employees.delete", p => p.Requirements.Add(new PermissionRequirement("employees.delete")))
            .AddPolicy("Permission:tenant.manage_roles", p => p.Requirements.Add(new PermissionRequirement("tenant.manage_roles")))
            .AddPolicy("Permission:tenant.manage_settings", p => p.Requirements.Add(new PermissionRequirement("tenant.manage_settings")));

        return services;
    }
}