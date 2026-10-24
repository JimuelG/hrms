using Core.Interfaces;
using Hangfire;
using Infrastructure.Jobs;

namespace API.Jobs;
public class MaintenanceJobs(ITenantJobRunner runner, IRetentionService retention)
{
    [DisableConcurrentExecution(timeoutInSeconds: 900)]
    [AutomaticRetry(Attempts = 2)]
    public Task RunAlertScan(CancellationToken ct) =>
        runner.ForEachTenantAsync("alert-scan", (sp, token) => sp.GetRequiredService<IAlertService>().ScanAsync(token), ct);
    
    [DisableConcurrentExecution(timeoutInSeconds: 1800)]
    [AutomaticRetry(Attempts = 2)]
    public Task EnsureLeaveGrants(CancellationToken ct) =>
        runner.ForEachTenantAsync("leave-grants", (sp, token) => sp.GetRequiredService<ILeaveGrantSweep>().EnsureAllAsync(token), ct);

    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
    [AutomaticRetry(Attempts = 1)]
    public async Task RunRetention(CancellationToken ct) => await retention.RunAsync(ct);

    [DisableConcurrentExecution(timeoutInSeconds: 1800)]
    [AutomaticRetry(Attempts = 2)]
    public Task RemoveLeaverAccess(CancellationToken ct) =>
        runner.ForEachTenantAsync("leaver-access", (sp, token) => sp.GetRequiredService<ILeaverAccessSweep>().RunAsync(token), ct);
}