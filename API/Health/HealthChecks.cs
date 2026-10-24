using Core.Common;
using Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace API.Health;

public sealed class DatabaseHealthCheck(AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            return await db.Database.CanConnectAsync(ct)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database unreachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database unreachable.", ex);
        }
    }
}

public sealed class KeysHealthCheck(string keysPath) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var exists = Directory.Exists(keysPath);
        var count = exists ? Directory.GetFiles(keysPath, "key-*.xml").Length : 0;
        
        return Task.FromResult(KeysRules.State(exists, count) switch
        {
            KeysFolderState.Healthy => HealthCheckResult.Healthy(),
            KeysFolderState.Empty => HealthCheckResult.Degraded("No encryption keys yet. Back up the folder once one appears."),
            _ => HealthCheckResult.Unhealthy("The encryption keys folder is missing. Stored ID number can't be read.")
        });
    }
}