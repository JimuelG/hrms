using System.ComponentModel;
using Core.Enums;

namespace Core.Common;
public static class ReportRangeRules
{
    public const int MaxDays = 93;

    public static string? Check(DateOnly from, DateOnly to)
    {
        if (from.Year < LeaveCalculator.MinYear || to.Year > LeaveCalculator.MaxYear) return "The dates are out of range.";
        if (to < from) return "The end date can't be before the start date.";
        if (to.DayNumber - from.DayNumber + 1 > MaxDays) return $"Choose at most {MaxDays} days at a time.";
        return null;
    }
}

public sealed record AttendanceDayFact(DateOnly Date, TimeOnly ClockInLocal, TimeOnly? ClockOutLocal, TimeOnly? ScheduledStart, TimeOnly? ScheduledEnd);

    public sealed record AttendanceSummaryInput(
        (DateOnly Start, DateOnly End)? Window,
        WorkingDay WorkingDays,
        bool HasSchedule,
        IReadOnlyList<AttendanceDayFact> Days,
        IReadOnlySet<DateOnly> LeaveDays,
        IReadOnlySet<DateOnly> Holidays,
        int OvertimeMinutes,
        int GraceMinutes,
        DateOnly Today);

    public sealed record AttendanceSummary(
        int ScheduledDays,
        int PresentDays,
        int AbsentDays,
        int LateCount,
        int LateMinutes,
        int UndertimeMinutes,
        int OpenRecords,
        int OvertimeMinutes,
        bool NoSchedule);

    public static class AttendanceSummaryRules
    {
        public static AttendanceSummary Summarize(AttendanceSummaryInput i)
        {
            if (i.Window is null) return new(0, 0, 0, 0, 0, 0, 0, 0, !i.HasSchedule);
            var (start, end) = i.Window.Value;

            var days = i.Days.Where(x => x.Date >= start && x.Date <= end)
                            .GroupBy(x => x.Date).Select(g => g.First()).ToList();

            var scheduled = 0;
            for (var day = start; day <= end; day = day.AddDays(1))
                if (i.WorkingDays.HasFlag(day.DayOfWeek.ToWorkingDay()) && !i.Holidays.Contains(day)) scheduled++;

            var absent = AttendanceDeductionRules.AbsentDates(
                start, end, i.WorkingDays, days.Select(x => x.Date).ToHashSet(), i.LeaveDays, i.Holidays).Count;

            int lateCount = 0, lateMinutes = 0, undertime = 0, open = 0;
            foreach (var rec in days)
            {
                if (rec.ScheduledStart is TimeOnly s)
                {
                    var late = AttendanceDeductionRules.LateMinutes(s, rec.ClockInLocal, i.GraceMinutes);
                    if (late > 0) { lateCount++; lateMinutes += late; }
                }

                if (rec.ClockOutLocal is TimeOnly outTime)
                {
                    if (rec.ScheduledEnd is TimeOnly e)
                        undertime += AttendanceDeductionRules.UndertimeMinutes(e, outTime, i.GraceMinutes);
                }
                else if (rec.Date < i.Today)
                {
                    open++;
                }
            }

            return new(scheduled, days.Count, absent, lateCount, lateMinutes, undertime, open, i.OvertimeMinutes, !i.HasSchedule);
        }
    }

public sealed record LeaveRequestFact(Guid EmployeeId, Guid LeaveTypeId, ApprovalStatus Status, decimal Days);
public sealed record LeaveUsageRow(Guid EmployeeId, Guid LeaveTypeId, decimal Approved, decimal Pending);
public sealed record LeaveTypeTotal(Guid LeaveTypeId, decimal Approved, decimal Pending);

public static class LeaveUsageRules
{
    public static IReadOnlyList<LeaveUsageRow> Usage(IEnumerable<LeaveRequestFact> facts) =>
        facts.Where(f => f.Status is ApprovalStatus.Approved or ApprovalStatus.Pending)
            .GroupBy(f => (f.EmployeeId, f.LeaveTypeId))
            .Select(g => new LeaveUsageRow(g.Key.EmployeeId, g.Key.LeaveTypeId,
                g.Where(f => f.Status == ApprovalStatus.Approved).Sum(f => f.Days),
                g.Where(f => f.Status == ApprovalStatus.Pending).Sum(f => f.Days)))
            .ToList();

    public static IReadOnlyList<LeaveTypeTotal> TypeTotals(IEnumerable<LeaveUsageRow> rows) =>
        rows.GroupBy(r => r.LeaveTypeId)
            .Select(g => new LeaveTypeTotal(g.Key, g.Sum(r => r.Approved), g.Sum(r => r.Pending)))
            .ToList();
}

public sealed record AwayRange(string Name, DateOnly Start, DateOnly End);
public sealed record CalendarDay(DateOnly Date, IReadOnlyList<string> Names);

public static class LeaveCalendarRules
{
    public static IReadOnlyList<CalendarDay> Days(IEnumerable<AwayRange> away, DateOnly from, DateOnly to)
    {
        var list = away.ToList();
        var result = new List<CalendarDay>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var names = list.Where(a => a.Start <= day && a.End >= day)
                            .Select(a => a.Name).Distinct()
                            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
            if (names.Count > 0) result.Add(new CalendarDay(day, names));
        }

        return result;
    }
}