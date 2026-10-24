using System.Text.Json;
using Core.Common;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

public sealed class LeaverAccessSweep(
    AppDbContext db,
    ITenantContext tenant,
    TimeProvider clock) : ILeaverAccessSweep
{
    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        var tenantId = tenant.RequiredTenantId;
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var settings = await db.Set<TenantSettings>().FirstOrDefaultAsync(ct);
        var today = LocalClock.LocalDate(nowUtc, settings?.TimeZoneId);

        var leavers = (await db.Set<Employee>()
                .Where(e => e.UserId != null && e.LastWorkingDay != null
                        && (e.Status == EmployeeStatus.Resigned || e.Status == EmployeeStatus.Terminated))
                .ToListAsync(ct))
            .Where(e => AccountRules.ShouldLoseAccess(e.Status, e.LastWorkingDay, today))
            .Select(e => e.UserId!.Value).ToList();
        if (leavers.Count == 0) return 0;

        var memberships = await db.UserTenants
            .Where(ut => ut.TenantId == tenantId && ut.IsActive && leavers.Contains(ut.UserId))
            .ToListAsync(ct);

        foreach (var m in memberships)
        {
            m.IsActive = false;
            db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = null,
                EntityName = "LoginAccount",
                EntityId = m.UserId.ToString(),
                Action = "AccessRemoved",
                NewValues = JsonSerializer.Serialize(new { Reason = "Left the company" }),
                TimestampUtc = nowUtc
            });
        }
        
        var ids = memberships.Select(m => m.UserId).ToList();
        await db.RefreshTokens.Where(t => t.TenantId == tenantId && ids.Contains(t.UserId) && t.RevokedAtUtc == null)     
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, nowUtc), ct);

        await db.SaveChangesAsync(ct);
        return memberships.Count;
    }
}