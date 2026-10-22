using Core.Common;
using Core.Constant;
using Core.DTOs.Dashboard;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Dashboard;

public sealed class DashboardService(
    IGenericRepository<Employee> employees,
    IGenericRepository<AttendanceRecord> attendance,
    IGenericRepository<LeaveRequest> leave,
    IGenericRepository<Holiday> holidays,
    IGenericRepository<WorkSchedule> schedules,
    IGenericRepository<TenantSettings> tenantSettings,
    IGenericRepository<JobPosting> postings,
    IApprovalQueryService approvals,
    IPermissionService permissions,
    ITenantContext tenant,
    TimeProvider clock) : IDashboardService
{
    private const int GraceMinutes = 10;
    private const int UpcomingDays = 7;
    private const int ListLimit = 10;

    public async Task<ServiceResult<DashboardDto>> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var ts = await tenantSettings.GetEntityWithSpec(new TenantSettingsSpecification(), ct);
        var zone = ResolveZone(ts?.TimeZoneId);
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(clock.GetUtcNow().UtcDateTime, zone);
        var today = DateOnly.FromDateTime(nowLocal);
        var nowTime = TimeOnly.FromDateTime(nowLocal);
        var companyDays = ts?.WorkingDays ?? new TenantSettings().WorkingDays;

        var codes = (await permissions.GetPermissionsAsync(userId, tenant.RequiredTenantId, ct)).ToHashSet();
        var me = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);

        var hasReports = me is not null && await employees.CountAsync(new DirectReportsSpecification(me.Id), ct) > 0;
        var scope = DashboardScope.Resolve(codes.Contains(Permissions.Employees.Read), hasReports);

        List<Employee> people = scope switch
        {
            DashboardScopeKind.Company => (await employees.ListAsync(new WorkingCandidatesSpecification(today, null), ct)).ToList(),
            DashboardScopeKind.Team => (await employees.ListAsync(new WorkingCandidatesSpecification(today, me!.Id), ct)).ToList(),
            _ => me is null ? [] : [me]
        };

        people = people.Where(p => HeadcountRules.IsWorking(p.Status, p.HireDate, p.LastWorkingDay, today)).ToList();

        var allSchedules = (await schedules.ListAsync(new DashboardSchedulesSpecification(), ct)).ToDictionary(s => s.Id);
        var holidayToday = (await holidays.ListAsync(new HolidaysInRangeSpecification(today, today), ct)).Count > 0;
        var records = (await attendance.ListAsync(new AttendanceInPeriodSpecification(today, today), ct))
            .ToDictionary(a => a.EmployeeId);

        var peopleIds = people.Select(p => p.Id).ToHashSet();
        var leaves = (await leave.ListAsync(new ApprovedLeaveSpecification(today, today.AddDays(UpcomingDays)), ct))
            .Where(r => peopleIds.Contains(r.EmployeeId)).ToList();
        var onLeaveToday = leaves
            .Where(r => LeaveWindows.Classify(r.StartDate, r.EndDate, today, UpcomingDays) == LeaveWindow.Today)
            .Select(r => r.EmployeeId).ToHashSet();

        PresenceStatus Presence(Employee p)
        {
            allSchedules.TryGetValue(p.ScheduleId ?? Guid.Empty, out var sched);
            var days = sched?.WorkingDays ?? companyDays;
            var worksToday = !holidayToday && days.HasFlag(today.DayOfWeek.ToWorkingDay());

            TimeOnly? clockIn = null;
            if (records.TryGetValue(p.Id, out var rec))
                clockIn = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(rec.ClockInUtc, DateTimeKind.Utc), zone));
        
            return PresenceClassifier.Classify(new PresenceInput(
                    worksToday, onLeaveToday.Contains(p.Id), sched is not null, sched?.StartTime, clockIn, nowTime, GraceMinutes));
        }

        string Name(Employee p) => $"{p.FirstName} {p.LastName}";
        var names = people.ToDictionary(p => p.Id, Name);

        HeadcountCard? headcount = null;
        AttendanceCard? attendanceCard = null;
        if (scope != DashboardScopeKind.Personal)
        {
            var monthStart = new DateOnly(today.Year, today.Month, 1);
            headcount = new HeadcountCard(
                people.Count,
                scope == DashboardScopeKind.Company ? await employees.CountAsync(new HiredBetweenSpecification(monthStart, today), ct) : null,
                scope == DashboardScopeKind.Company ? await employees.CountAsync(new LeftBetweenSpecification(monthStart, today), ct) : null);
            
            var rows = people.Select(p => (Person: p, Status: Presence(p))).ToList();
            var tally = PresenceClassifier.Tally(rows.Select(r => r.Status));
            attendanceCard = new AttendanceCard(
                tally.Expected,
                tally.Present,
                tally.Late,
                tally.OnLeave,
                tally.NotYetIn,
                tally.NotIn,
                tally.NoSchedule,
                rows.Where(r => r.Status == PresenceStatus.Late).Select(r => Name(r.Person)).Take(ListLimit).ToList(),
                rows.Where(r => r.Status == PresenceStatus.NotIn).Select(r => Name(r.Person)).Take(ListLimit).ToList());
        }

        AwayDto Away(LeaveRequest r) => new(names.GetValueOrDefault(r.EmployeeId, "Unknown"), r.StartDate, r.EndDate);
        var leaveCard = new LeaveCard(
            leaves.Where(r => LeaveWindows.Classify(r.StartDate, r.EndDate, today, UpcomingDays) == LeaveWindow.Today)
                .OrderBy(r => r.EndDate).Take(ListLimit).Select(Away).ToList(),
            leaves.Where(r => LeaveWindows.Classify(r.StartDate, r.EndDate, today, UpcomingDays) == LeaveWindow.Upcoming)
                .OrderBy(r => r.StartDate).Take(ListLimit).Select(Away).ToList());

        MeCard? meCard = me is null ? null : new MeCard((int)Presence(me), await leave.CountAsync(new PendingLeaveByEmployeeSpecification(me.Id), ct));

        var inbox = await approvals.GetInboxAsync(userId, 1, 1, ct);
        var pending = inbox.Succeeded ? inbox.Value!.Total : 0;

        int? openPostings = scope == DashboardScopeKind.Company
            ? await postings.CountAsync(new OpenJobPostingsSpecification(), ct) : null;

        return ServiceResult<DashboardDto>.Success(new DashboardDto(scope.ToString(), today, headcount, pending, attendanceCard, leaveCard, openPostings, meCard));
    }

    private static TimeZoneInfo ResolveZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return TimeZoneInfo.Utc;
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception)
        {
            return TimeZoneInfo.Utc;
        }
    }
}