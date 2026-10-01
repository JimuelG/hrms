using Core.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace Tests;

public class TenantIsolationFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder().Build();
    public string ConnectionString => _sql.GetConnectionString();

    private sealed class NullCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => null;
        public string? IpAddress => null;
        public string? UserAgent => null;
    }

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();

        var tenantCtx = new TenantContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString).Options;
        await using var db = new AppDbContext(options, tenantCtx);
        await db.Database.MigrateAsync();
    }
    public async Task DisposeAsync() => await _sql.DisposeAsync();

    public AppDbContext CreateContextAs(Guid? tenantId)
    {
        var tenantCtx = new TenantContext();
        if (tenantId is Guid id) tenantCtx.SetTenant(id);

        var tenantInterceptor = new TenantSaveChangesInterceptor(tenantCtx);
        var auditInterceptor = new AuditSaveChangesInterceptor(new NullCurrentUserService(), tenantCtx);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .AddInterceptors(tenantInterceptor, auditInterceptor)
            .Options;
        
        return new AppDbContext(options, tenantCtx);
    }
}

[CollectionDefinition("TenantIsolation")]
public class TenantIsolationCollenction : ICollectionFixture<TenantIsolationFixture> {}