using Core.Constant;
using Core.Entities;
using Infrastructure.Identity;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence;
public static class DevDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var tenantInit = scope.ServiceProvider.GetRequiredService<ITenantContextInitializer>();

        var password = config["Seed:Password"]
            ?? throw new InvalidOperationException("Set Seed:Password in user-secrets.");
        
        async Task<Tenant> EnsureTenant(string slug, string name)
        {
            var t = await db.Tenants.FirstOrDefaultAsync(x => x.Slug == slug);
            if (t is null) { t = new Tenant { Name = name, Slug = slug }; db.Tenants.Add(t); await db.SaveChangesAsync(); }
            return t;
        }

        async Task<ApplicationUser> EnsureUser(string email, bool plaformAdmin, params Tenant[] memberOf)
        {
            var existing = await users.FindByEmailAsync(email);

            if (existing is not null) return existing;

            var u = new ApplicationUser
            {
                UserName = email, 
                Email = email, 
                EmailConfirmed = true,
                FirstName = "Dev", 
                LastName = email.Split('@')[0], 
                IsPlatformAdmin = plaformAdmin 
            };

            var r = await users.CreateAsync(u, password);
            if (!r.Succeeded) throw new InvalidOperationException(string.Join("; ", r.Errors.Select(e => e.Description)));
            foreach (var t in memberOf) db.UserTenants.Add(new UserTenant { UserId = u.Id, TenantId = t.Id });
            await db.SaveChangesAsync();
            return u;
        }

        var acme = await EnsureTenant("acme", "Acme Corp");
        var globex = await EnsureTenant("globex", "Globex Inc");

        var acmeAdmin = await EnsureUser("admin@acme.test", false, acme);
        await EnsureUser("multi@hrms.test", false, globex);
        await EnsureUser("platform@hrms.test", true);

        var existingCodes = await db.Permissions.Select(p => p.Code).ToListAsync();
        var missing = Permissions.All().Where(p => !existingCodes.Contains(p.Code)).ToList();
        if (missing.Count > 0)
        {
            db.Permissions.AddRange(missing.Select(p => new Permission
            {
                Code = p.Code,
                Module = p.Module,
                Description = p.Description
            }));
            await db.SaveChangesAsync();
        }

        tenantInit.SetTenant(acme.Id);
        await EnsureTenantRole(db, acme, "HR Admin", isSystemRole: true,
        [
           Permissions.Employees.Read,
           Permissions.Employees.Write,
           Permissions.Employees.Delete,
           Permissions.Tenant.ManageRoles,
           Permissions.Tenant.ManageSettings,
           Permissions.Branches.Read,
           Permissions.Branches.Write,
           Permissions.Branches.Delete,
           Permissions.Departments.Read,
           Permissions.Departments.Write,
           Permissions.Departments.Delete
        ], assignTo:acmeAdmin);
    }

    private static async Task EnsureTenantRole(
        AppDbContext db, Tenant tenant, string roleName, bool isSystemRole,
        string[] permissionCodes, ApplicationUser assignTo)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.TenantId == tenant.Id && r.Name == roleName);
        // var role = await db.Roles.FirstOrDefaultAsync(r => r.TenantId == tenant.Id && r.Name == roleName);
        if (role is null)
        {
            role = new Role { TenantId = tenant.Id, Name = roleName, IsSystemRole = isSystemRole };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
        }

        var permissionIds = await db.Permissions
            .Where(p => permissionCodes.Contains(p.Code))
            .Select(p => p.Id)
            .ToListAsync();

        var existingLinks = await db.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        var toAdd = permissionIds.Except(existingLinks);
        foreach (var permId in toAdd)
        {
            db.RolePermissions.Add(new RolePermission { TenantId = tenant.Id, RoleId = role.Id, PermissionId = permId });
        }

        var alreadyAssigned = await db.UserTenantRoles.AnyAsync(utr =>
            utr.UserId == assignTo.Id && utr.TenantId == tenant.Id && utr.RoleId == role.Id);
        if (!alreadyAssigned)
            db.UserTenantRoles.Add(new UserTenantRole { UserId = assignTo.Id, TenantId = tenant.Id, RoleId = role.Id });

        await db.SaveChangesAsync();
    }
}