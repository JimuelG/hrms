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
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Department> Departments => Set<Department>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenantConfiguration).Assembly);

        ApplyGlobalFilters(modelBuilder);
    }

    private static readonly MethodInfo SetTenantFilterMethod =
        typeof(AppDbContext).GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo SetTenantAndSoftDeleteFilterMethod =
        typeof(AppDbContext).GetMethod(nameof(SetTenantAndSoftDeleteFilterMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private void ApplyGlobalFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var isTenant = typeof(ITenantEntity).IsAssignableFrom(clrType);
            var isSoftDelete = typeof(ISoftDelete).IsAssignableFrom(clrType);

            if (isTenant && isSoftDelete)
                SetTenantAndSoftDeleteFilterMethod.MakeGenericMethod(clrType).Invoke(this, new object[] { builder });
            else if (isTenant)
                SetTenantFilterMethod.MakeGenericMethod(clrType).Invoke(this, new object[] { builder });
        }
    }

    private void SetTenantFilter<T>(ModelBuilder builder) where T : class, ITenantEntity
    {
        builder.Entity<T>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        builder.Entity<T>().HasIndex(e => e.TenantId);
    }

    private void SetTenantAndSoftDeleteFilter<T>(ModelBuilder builder) where T : class, ITenantEntity, ISoftDelete
    {
        builder.Entity<T>().HasQueryFilter(e => e.TenantId == CurrentTenantId && !e.IsDeleted);
        builder.Entity<T>().HasIndex(e => e.TenantId);
    }
}