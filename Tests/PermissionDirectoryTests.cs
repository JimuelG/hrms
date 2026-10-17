using Core.Entities;
using Infrastructure.Authorization;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Tenancy;

namespace Tests;

[Collection("TenantIsolation")]
public class PermissionDirectoryTests(TenantIsolationFixture fixture)
{
    private sealed record World(Guid TenantA, Guid TenantB, string Code, Guid Holder, Guid InactiveMember, Guid NoRole, Guid OtherTenantHolder);

    private static ApplicationUser NewUser(string name) => new()
    {
        UserName = $"{name}-{Guid.NewGuid():N}", Email = $"{name}-{Guid.NewGuid():N}@test.local",
        FirstName = name, LastName = "Test"
    };

    private static PermissionDirectory DirectoryFor(AppDbContext db, Guid tenantId)
    {
        var context = new TenantContext();
        context.SetTenant(tenantId);
        return new PermissionDirectory(db, context);
    }

    private async Task<World> ArrangeAsync()
    {
        var code = $"test.{Guid.NewGuid():N}";
        var tenantA = new Tenant { Name = "A", Slug = $"a-{Guid.NewGuid():N}" };
        var tenantB = new Tenant { Name = "B", Slug = $"b-{Guid.NewGuid():N}" };
        var permission = new Permission { Code = code, Module = "Test", Description = "Test permission" };
        var holder = NewUser("holder");
        var inactive = NewUser("inactive");
        var noRole = NewUser("norole");
        var other = NewUser("other");

        await using (var platform = fixture.CreateContextAs(null))
        {
            platform.AddRange(tenantA, tenantB, permission, holder, inactive, noRole, other);
            await platform.SaveChangesAsync();

            platform.UserTenants.AddRange(
                new UserTenant { UserId = holder.Id, TenantId = tenantA.Id },
                new UserTenant { UserId = inactive.Id, TenantId = tenantA.Id, IsActive = false },
                new UserTenant { UserId = noRole.Id, TenantId = tenantA.Id },
                new UserTenant { UserId = other.Id, TenantId = tenantB.Id });
            await platform.SaveChangesAsync();
        }

        Guid roleA, roleB;
        await using (var inA = fixture.CreateContextAs(tenantA.Id))
        {
            var role = new Role { Name = "Approver" };
            inA.Roles.Add(role);
            await inA.SaveChangesAsync();
            inA.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
            await inA.SaveChangesAsync();
            roleA = role.Id;
        }
        await using (var inB = fixture.CreateContextAs(tenantB.Id))
        {
            var role = new Role { Name = "Approver" };
            inB.Roles.Add(role);
            await inB.SaveChangesAsync();
            inB.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
            await inB.SaveChangesAsync();
            roleB = role.Id;
        }

        await using (var platform = fixture.CreateContextAs(null))
        {
            platform.UserTenantRoles.AddRange(
                new UserTenantRole { UserId = holder.Id, TenantId = tenantA.Id, RoleId = roleA },
                new UserTenantRole { UserId = inactive.Id, TenantId = tenantA.Id, RoleId = roleA },
                new UserTenantRole { UserId = other.Id, TenantId = tenantB.Id, RoleId = roleB });
            await platform.SaveChangesAsync();
        }

        return new World(tenantA.Id, tenantB.Id, code, holder.Id, inactive.Id, noRole.Id, other.Id);
    }

    [Fact]
    public async Task Only_Active_Members_Who_Hold_The_Permission_In_This_Tenant_Are_Returned()
    {
        var w = await ArrangeAsync();
        await using var db = fixture.CreateContextAs(w.TenantA);

        var result = await DirectoryFor(db, w.TenantA).GetHolderUserIdsAsync([w.Code]);

        Assert.Equal(new[] { w.Holder }, result[w.Code].ToArray());   // not the inactive member, the role-less member or the other tenant's user
    }

    [Fact]
    public async Task Another_Tenant_Sees_Only_Its_Own_Holders()
    {
        var w = await ArrangeAsync();
        await using var db = fixture.CreateContextAs(w.TenantB);

        var result = await DirectoryFor(db, w.TenantB).GetHolderUserIdsAsync([w.Code]);

        Assert.Equal(new[] { w.OtherTenantHolder }, result[w.Code].ToArray());
    }

    [Fact]
    public async Task A_Permission_Nobody_Holds_Is_Absent()
    {
        var w = await ArrangeAsync();
        await using var db = fixture.CreateContextAs(w.TenantA);

        var result = await DirectoryFor(db, w.TenantA).GetHolderUserIdsAsync([$"test.{Guid.NewGuid():N}"]);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Asking_For_No_Permissions_Returns_Nothing()
    {
        var w = await ArrangeAsync();
        await using var db = fixture.CreateContextAs(w.TenantA);

        Assert.Empty(await DirectoryFor(db, w.TenantA).GetHolderUserIdsAsync([]));
    }
}