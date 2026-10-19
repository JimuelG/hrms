using Core.Enums;

namespace Core.Common;
public static class LeaveInPeriod
{
    public static decimal Days(IEnumerable<LeaveDayLine> lines, PayPeriod period) =>
        lines.Where(l => l.Status == LeaveDayStatus.Counted && l.Date >= period.Start && l.Date <= period.End).Sum(l => l.Value);

    public static int WorkingDaysPerWeek(WorkingDay days)
    {
        var count = 0;
        foreach (var d in Enum.GetValues<DayOfWeek>())
        {
            if (days.HasFlag(d.ToWorkingDay())) count++;
        }
        return count >= 6 ? 6 : 5;
    }
}