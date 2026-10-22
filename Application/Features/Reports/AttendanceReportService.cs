using Core.Common;
using Core.DTOs.Reports;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Reports;

public sealed class AttendanceReportService(
    IGenericRepository<Employee> employees,
    IGenericRepository<AttendanceRecord> attendance,
    IGenericRepository<OvertimeRequest> overtime,
    IGenericRepository<LeaveRequest> leave,
    IGenericRepository<Holiday> holidays,
    IGenericRepository<WorkSchedule> schedules,
    IGenericRepository<TenantSettings> tenantSettings,
    ITimeZoneResolver zones,
    TimeProvider clock) : IAttendanceReportService
{
    public async Task<ServiceResult<CsvFile>> ExportAsync(AttendanceReportQuery q, CancellationToken ct = default)
    {
        var built = await BuildAsync(q, ct);
        if (!built.Succeeded) return ServiceResult<CsvFile>.Fail(built.Error!, built.ErrorType);
        var r = built.Value!;

        var csv = WorkforceRules.ToCsv(
            ["Employee number", "Name", "Department", "Branch", "Scheduled days", "Present days", "Absent days",
            "Late count", "Late minutes", "Undertime minutes", "Overtime minutes", "Open records", "No schedule"],
            r.Rows.Select(x => new string?[]
            {
                x.EmployeeNumber,
                x.Name,
                x.Department,
                x.Branch,
                x.ScheduledDays.ToString(),
                x.PresentDays.ToString(),
                x.AbsentDays.ToString(),
                x.LateCount.ToString(),
                x.LateMinutes.ToString(),
                x.UndertimeMinutes.ToString(),
                x.OvertimeMinutes.ToString(),
                x.OpenRecords.ToString(),
                x.NoSchedule ? "Yes" : "No"
            }));

        return ServiceResult<CsvFile>.Success(new CsvFile($"attedance-{r.From:yyyyMMdd}-{r.To:yyyyMMdd}.csv" , csv));
    }

    public Task<ServiceResult<AttendanceReportDto>> GetAsync(AttendanceReportQuery q, CancellationToken ct = default) =>
        BuildAsync(q, ct);

    private async Task<ServiceResult<AttendanceReportDto>> BuildAsync(AttendanceReportQuery q, CancellationToken ct)
    {
        var from = q.From!.Value;
        var to = q.To!.Value;
        var error = ReportRangeRules.Check(from, to);
        if (error is not null) return ServiceResult<AttendanceReportDto>.Fail(error);

        var ts = await tenantSettings.GetEntityWithSpec(new TenantSettingsSpecification(), ct);
        var today = LocalClock.LocalDate(clock.GetUtcNow().UtcDateTime, ts?.TimeZoneId);
        var companyDays = ts?.WorkingDays ?? new TenantSettings().WorkingDays;
        var period = new PayPeriod(from, to);

        var people = await employees.ListAsync(new AttendanceReportEmployeesSpecification(from, to, q.DepartmentId, q.BranchId), ct);
        var allSchedules = (await schedules.ListAsync(new DashboardSchedulesSpecification(), ct)).ToDictionary(s => s.Id);
        var holidayDates = (await holidays.ListAsync(new HolidaysInRangeSpecification(from, to), ct)).Select(h => h.Date).ToHashSet();

        var records = (await attendance.ListAsync(new AttendanceInPeriodSpecification(from, to), ct))
            .GroupBy(a => a.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());
        var overtimeBy = (await overtime.ListAsync(new ApprovedOvertimeInRangeSpecification(from, to), ct))
            .GroupBy(o => o.EmployeeId).ToDictionary(g => g.Key, g => g.Sum(o => o.ApprovedMinutes ?? 0));
        var leaveBy = (await leave.ListAsync(new ApprovedLeaveSpecification(from, to), ct))
            .GroupBy(r => r.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        var rows = new List<AttendanceRowDto>();
        foreach (var person in people)
        {
            WorkSchedule? schedule = null;
            if (person.ScheduleId is Guid sid) allSchedules.TryGetValue(sid, out schedule);

            var zone = ResolveZone(await zones.ResolveAsync(person, ct));
            var facts = (records.GetValueOrDefault(person.Id) ?? []).Select(a => new AttendanceDayFact(
                a.Date,
                Local(a.ClockInUtc, zone),
                a.ClockOutUtc is DateTime o ? Local(o, zone) : null,
                a.ScheduledStart,
                a.ScheduledEnd)).ToList();

            var leaveDates = new HashSet<DateOnly>();
            foreach (var req in leaveBy.GetValueOrDefault(person.Id) ?? [])
                for (var d = req.StartDate > from ? req.StartDate : from; d <= (req.EndDate < to ? req.EndDate : to); d = d.AddDays(1))
                    leaveDates.Add(d);

            var s = AttendanceSummaryRules.Summarize(new AttendanceSummaryInput(
                PayPeriodCalculator.EmployedWindow(period, person.HireDate, person.LastWorkingDay),
                schedule?.WorkingDays ?? companyDays,
                schedule is not null,
                facts,
                leaveDates,
                holidayDates,
                overtimeBy.GetValueOrDefault(person.Id),
                q.GraceMinutes,
                today));
            
            rows.Add(new AttendanceRowDto(
                person.Id,
                person.EmployeeNumber,
                $"{person.FirstName} {person.LastName}",
                person.Department?.Name,
                person.Branch?.Name,
                s.ScheduledDays,
                s.PresentDays,
                s.AbsentDays,
                s.LateCount,
                s.LateMinutes,
                s.UndertimeMinutes,
                s.OvertimeMinutes,
                s.OpenRecords,
                s.NoSchedule));
        }
            var totals = new AttendanceTotalsDto(
                rows.Sum(r => r.ScheduledDays),
                rows.Sum(r => r.PresentDays),
                rows.Sum(r => r.AbsentDays),
                rows.Sum(r => r.LateCount),
                rows.Sum(r => r.LateMinutes),
                rows.Sum(r => r.UndertimeMinutes),
                rows.Sum(r => r.OvertimeMinutes),
                rows.Sum(r => r.OpenRecords));

            return ServiceResult<AttendanceReportDto>.Success(new AttendanceReportDto(from, to, q.GraceMinutes, rows, totals, rows.Count(r => r.NoSchedule)));
    }

    private static TimeOnly Local(DateTime utc, TimeZoneInfo zone) =>
        TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone));

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