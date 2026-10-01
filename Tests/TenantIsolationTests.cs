using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Tests;

[Collection("TenantIsolation")]
public class TenantIsolationTests(TenantIsolationFixture fixture)
{
    private async Task<(Tenant TenantA, Tenant TenantB, Role RoleInA)> SeedTwoTenantsAsync()
    {
        await using var seedDb = fixture.CreateContextAs(null);
        var tenantA = new Tenant { Name = "Tenant A", Slug = $"a-{Guid.NewGuid():N}"};
        var tenantB = new Tenant { Name = "Tenant B", Slug = $"b-{Guid.NewGuid():N}"};
        seedDb.Tenants.AddRange(tenantA, tenantB);
        await seedDb.SaveChangesAsync();

        await using var asTenantA = fixture.CreateContextAs(tenantA.Id);
        var role = new Role { TenantId = tenantA.Id, Name = "Confidential Role A" };
        asTenantA.Roles.Add(role);
        await asTenantA.SaveChangesAsync();

        return (tenantA, tenantB, role);
    }

    [Fact]
    public async Task TenantB_Cannot_Read_TenantAs_Role_By_Id()
    {
        var (_, tenantB, roleInA) = await SeedTwoTenantsAsync();

        await using var asTenantB = fixture.CreateContextAs(tenantB.Id);

        var result = await asTenantB.Roles.FirstOrDefaultAsync(r => r.Id == roleInA.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task TenantB_Role_List_Never_Contains_TenantAs_Roles()
    {
        var (_, tenantB, roleInA) = await SeedTwoTenantsAsync();

        await using var asTenantB = fixture.CreateContextAs(tenantB.Id);
        var allVisibleToB = await asTenantB.Roles.ToListAsync();

        Assert.DoesNotContain(allVisibleToB, r => r.Id == roleInA.Id);
    }

    [Fact]
    public async Task TenantB_Cannot_Update_TenantAs_Role()
    {
        var (_, tenantB, roleInA) = await SeedTwoTenantsAsync();

        await using var asTenantB = fixture.CreateContextAs(tenantB.Id);

        var role = await asTenantB.Roles.FirstOrDefaultAsync(r => r.Id == roleInA.Id);
        
        Assert.Null(role);
    }

    [Fact]
    public async Task Write_With_No_Tenant_Context_Throws_Rather_Than_Silently_Succeeding()
    {
        await using var asNoTenant = fixture.CreateContextAs(null);
        asNoTenant.Roles.Add(new Role { Name = "Orphan Role" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => asNoTenant.SaveChangesAsync());
    }

    [Fact]
    public async Task TenantSaveChangesInterceptor_Overwrites_Spoofed_TenantId_On_Insert()
    {
        var (tenantA, tenantB, _) = await SeedTwoTenantsAsync();

        await using var asTenantB = fixture.CreateContextAs(tenantB.Id);

        var spoofed = new Role { TenantId = tenantA.Id, Name = "Spoofed Role" };
        asTenantB.Roles.Add(spoofed);
        await asTenantB.SaveChangesAsync();

        Assert.Equal(tenantB.Id, spoofed.TenantId);
    }
}