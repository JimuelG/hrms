using Infrastructure.Persistence;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Testcontainers.MsSql;

namespace Tests;

public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();

        // Program.cs seeds on startup but doesn't migrate, so build the schema first.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_sql.GetConnectionString()).Options;
        await using var db = new AppDbContext(options, new TenantContext());
        await db.Database.MigrateAsync();
    }

    // WebApplicationFactory already defines DisposeAsync() returning ValueTask,
    // so xunit's Task-returning one is implemented explicitly.
    async Task IAsyncLifetime.DisposeAsync() => await _sql.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _sql.GetConnectionString(),
                // Test-only values, so the suite doesn't depend on anyone's local user-secrets.
                ["Seed:Password"] = "Password123!",
                ["Jwt:SigningKey"] = "test-only-signing-key-0123456789-abcdefghijklmnop"
            }));
    }
}