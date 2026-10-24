using Core.Common;
using Core.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs;

public sealed class RetentionService(
    AppDbContext db,
    IConfiguration config,
    TimeProvider clock,
    ILogger<RetentionService> log) : IRetentionService
{
    private const int BatchSize = 5000;
    public async Task<(int AuditRows, int Alerts)> RunAsync(CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;

        var auditDeleted = 0;
        var auditDays = config.GetValue("Retention:AuditDays", RetentionRules.DefaultAuditDays);
        if (RetentionRules.Cutoff(now, auditDays, RetentionRules.MinimumAuditDays) is DateTime auditCutoff)
        {
            int batch;
            do
            {
                batch = await db.AuditLogs.Where(a => a.TimestampUtc < auditCutoff).Take(BatchSize).ExecuteDeleteAsync(ct);
                auditDeleted += batch;
            } while (batch == BatchSize);
        }

        var alertsDeleted = 0;
        if (RetentionRules.Cutoff(now, RetentionRules.ResolvedAlertDays, 1) is DateTime alertCutoff)
            alertsDeleted = await db.Alerts.IgnoreQueryFilters()
                .Where(a => a.ResolvedAtUtc != null && a.ResolvedAtUtc < alertCutoff)
                .ExecuteDeleteAsync(ct);

        log.LogInformation("Retention removed {AuditRows} audit rows and {Alerts} resolved alerts", auditDeleted, alertsDeleted);
        return(auditDeleted, alertsDeleted);
    }
}