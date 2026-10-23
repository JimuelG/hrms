using Core.Interfaces;
using Hangfire;
using Infrastructure.Jobs;

namespace API.Jobs;
public class MaintenanceJobs(ITenantJobRunner runner)
{
    [DisableConcurrentExecution(timeoutInSeconds: 900)]
    [AutomaticRetry(Attempts = 2)]
    public Task RunAlertScan(CancellationToken ct) =>
        runner.ForEachTenantAsync("alert-scan", (sp, token) => sp.GetRequiredService<IAlertService>().ScanAsync(token), ct);
    
    [DisableConcurrentExecution(timeoutInSeconds: 1800)]
    [AutomaticRetry(Attempts = 2)]
    public Task EnsureLeaveGrants(CancellationToken ct) =>
        runner.ForEachTenantAsync("leave-grants", (sp, token) => sp.GetRequiredService<ILeaveGrantSweep>().EnsureAllAsync(token), ct);
}