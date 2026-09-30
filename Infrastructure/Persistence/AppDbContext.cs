using System.Reflection;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Config;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;
public class AppDbContext : IdentityUserContext<ApplicationUser, Guid>
{
    private readonly ITenantContext _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext) 
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    private Guid? CurrentTenantId => _tenantContext.TenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<UserTenant> UserTenants => Set<UserTenant>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserTenantRole> UserTenantRoles => Set<UserTenantRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenantConfiguration).Assembly);

        ApplyTenantFilters(modelBuilder);
    }

    private static readonly MethodInfo SetFilterMethod =
        typeof(AppDbContext).GetMethod(nameof(SetTenantFilter),
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        
    private void ApplyTenantFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes()
            .Where(t => typeof(ITenantEntity).IsAssignableFrom(t.ClrType)))
        {
            SetFilterMethod.MakeGenericMethod(entityType.ClrType).Invoke(this, new object[] { builder });
        }
    }

    private void SetTenantFilter<T>(ModelBuilder builder) where T : class, ITenantEntity
    {
        builder.Entity<T>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        builder.Entity<T>().HasIndex(e => e.TenantId);
    }
}