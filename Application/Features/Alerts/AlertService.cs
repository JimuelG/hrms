using Core.Common;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Alerts;

public class AlertService(
    IUnitOfWork unit,
    IGenericRepository<Alert> alerts,
    IGenericRepository<ApprovalRequest> approvals,
    IGenericRepository<EmployeeDocument> documents,
    IGenericRepository<AttendanceRecord> attendance,
    IGenericRepository<Employee> employees,
    IGenericRepository<TenantSettings> tenantSettings,
    TimeProvider clock) : IAlertService
{
    public async Task<IReadOnlyList<AlertDto>> ListOpenAsync(CancellationToken ct = default) =>
        (await alerts.ListAsync(new OpenAlertsSpecification(null), ct))
            .Select(a => new AlertDto(a.Id, (int)a.Kind, a.Title, a.Message, a.SubjectId, a.CreatedAtUtc)).ToList();

    public async Task<int> ScanAsync(CancellationToken ct = default)
    {
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var ts = await tenantSettings.GetEntityWithSpec(new TenantSettingsSpecification(), ct);
        var today = LocalClock.LocalDate(nowUtc, ts?.TimeZoneId);

        var staleApprovals = (await approvals.ListAsync(new PendingApprovalsSpecification(), ct))
            .Where(r => AlertRules.IsStaleApproval(r.LastActivityAtUtc, nowUtc, AlertRules.StaleApprovalDays)).ToList();
        var docs = (await documents.ListAsync(new DocumentsExpiringBySpecification(today.AddDays(AlertRules.DocumentWarnDays)), ct))
            .Select(d => (Doc: d, State: AlertRules.DocumentState(d.ExpirationDate, today, AlertRules.DocumentWarnDays)))
            .Where(x => x.State != DocumentExpiryState.None).ToList();
        var open = (await attendance.ListAsync(new OpenAttendanceBeforeSpecification(today.AddDays(-AlertRules.OpenAttendanceDays)), ct))
            .Where(a => AlertRules.IsOpenAttendance(a.Date, a.ClockOutUtc is not null, today, AlertRules.OpenAttendanceDays)).ToList();

        var ids = staleApprovals.Select(r => r.RequesterEmployeeId)
            .Concat(docs.Select(x => x.Doc.EmployeeId)).Concat(open.Select(a => a.EmployeeId)).Distinct().ToList();
        var names = ids.Count == 0 ? new Dictionary<Guid, string>()
            : (await employees.ListAsync(new EmployeesByIdsForPayrollSpecification(ids), ct))
                .ToDictionary(e => e.Id, e => $"{e.FirstName} {e.LastName}");
        string Name(Guid id) => names.GetValueOrDefault(id, "An employee");

        var desired = new List<DesiredAlert>();
        desired.AddRange(staleApprovals.Select(r => new DesiredAlert(AlertKind.StaleApproval, AlertRules.ApprovalKey(r.Id),
            "Approval waiting",
            $"A request from {Name(r.RequesterEmployeeId)} has had no activity for {(int)(nowUtc - r.LastActivityAtUtc).TotalDays} days.",
            r.Id)));
        desired.AddRange(docs.Select(x => new DesiredAlert(AlertKind.DocumentExpiry, AlertRules.DocumentKey(x.Doc.Id, x.State),
            x.State == DocumentExpiryState.Expired ? "Document expired" : "Document expiring soon",
            $"{Name(x.Doc.EmployeeId)}: {x.Doc.DocumentType} {(x.State == DocumentExpiryState.Expired ? "expired on" : "expires on")} {x.Doc.ExpirationDate:yyyy-MM-dd}.",
            x.Doc.EmployeeId)));
        desired.AddRange(open.Select(a => new DesiredAlert(AlertKind.OpenAttendance, AlertRules.AttendanceKey(a.Id),
            "No clock-out",
            $"{Name(a.EmployeeId)} has no clock-out for {a.Date:yyyy-MM-dd}. Correct the record if they did work.",
            a.EmployeeId)));

        var changed = 0;
        foreach (var kind in Enum.GetValues<AlertKind>())
        {
            var existing = await alerts.ListAsync(new OpenAlertsSpecification(kind), ct);
            var wanted = desired.Where(d => d.Kind == kind).GroupBy(d => d.Key).ToDictionary(g => g.Key, g => g.First());
            var plan = AlertRules.Reconcile(existing.Select(a => a.DedupeKey), wanted.Keys);

            foreach (var key in plan.Create)
            {
                var d = wanted[key];
                alerts.Add(new Alert { Kind = kind, DedupeKey = d.Key, Title = d.Title, Message = d.Message, SubjectId = d.SubjectId });
            }
            foreach (var row in existing.Where(a => plan.Resolve.Contains(a.DedupeKey))) row.ResolvedAtUtc = nowUtc;
            changed += plan.Create.Count + plan.Resolve.Count;
        }

        if (changed > 0) await unit.Complete();
        return changed;
    }
}