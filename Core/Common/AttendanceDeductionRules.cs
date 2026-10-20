using Core.Enums;

namespace Core.Common;
public static class AttendanceDeductionRules
{
    private static int Minutes(TimeOnly t) => t.Hour * 60 + t.Minute;
    public static int LateMinutes(TimeOnly scheduleStart, TimeOnly clockInLocal, int grace) =>
        Math.Max(0, Minutes(clockInLocal) - Minutes(scheduleStart) - grace);

    public static int UndertimeMinutes(TimeOnly scheduleEnd, TimeOnly clockOutLocal, int grace) =>
        Math.Max(0, Minutes(scheduleEnd) - Minutes(clockOutLocal) - grace);

    public static IReadOnlyList<DateOnly> AbsentDates(
        DateOnly windowStart, DateOnly windowEnd, WorkingDay workingDays,
        IReadOnlySet<DateOnly> attendedDates, IReadOnlySet<DateOnly> leaveDates, IReadOnlySet<DateOnly> holidays)
    {
        var absent = new List<DateOnly>();
        for (var d = windowStart; d <= windowEnd; d = d.AddDays(1))
        {
            if (!workingDays.HasFlag(d.DayOfWeek.ToWorkingDay())) continue;
            if (holidays.Contains(d) || attendedDates.Contains(d) || leaveDates.Contains(d)) continue;
            absent.Add(d);
        }

        return absent;
    }

    public static decimal Capped(decimal deduction, decimal basic) => Math.Min(deduction, Math.Max(0m, basic));
}