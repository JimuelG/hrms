using Core.Entities;
using Infrastructure.Persistence;
using Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs;

public interface ITenantJobRunner
{
    Task ForEachTenantAsync(string jobName, Func<IServiceProvider, CancellationToken, Task> work, CancellationToken ct);
}
public sealed class TenantJobRunner(IServiceScopeFactory scopes, ILogger<TenantJobRunner> log) : ITenantJobRunner
{
    public async Task ForEachTenantAsync(string jobName, Func<IServiceProvider, CancellationToken, Task> work, CancellationToken ct)
    {
        List<Guid> tenantIds;
        using (var scope = scopes.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            tenantIds = await db.Tenants.Where(t => t.Status == TenantStatus.Active).Select(t => t.Id).ToListAsync(ct);
        }

        foreach (var id in tenantIds)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var scope = scopes.CreateScope();
                scope.ServiceProvider.GetRequiredService<ITenantContextInitializer>().SetTenant(id);
                await work(scope.ServiceProvider, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Job {Job} failed for tenant {TenantId}", jobName, id);
            }
        }
    }
}