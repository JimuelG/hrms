using Core.Enums;

namespace Core.Common;
public static class OvertimeClassifier
{
    public static OvertimeDayType Classify(DateOnly date, WorkingDay workingDays, IReadOnlySet<DateOnly> holidays)
    {
        if (holidays.Contains(date)) return OvertimeDayType.Holiday;
        return workingDays.HasFlag(date.DayOfWeek.ToWorkingDay()) ? OvertimeDayType.Ordinary : OvertimeDayType.RestDay;
    }

    public static IReadOnlyList<OvertimeBucket> Bucket(IEnumerable<(OvertimeDayType Type, int Minutes)> items) =>
        items.Where(i => i.Minutes > 0)
            .GroupBy(i => i.Type)
            .Select(g => new OvertimeBucket(g.Key, g.Sum(x => x.Minutes)))
            .OrderBy(b => b.DayType)
            .ToList();

    public static (decimal Multiplier, bool FellBack) MultiplierFor(OvertimeDayType type, PayrollSettings s) => type switch
    {
        OvertimeDayType.RestDay when s.RestDayOvertimeMultiplier is decimal m => (m, false),
        OvertimeDayType.Holiday when s.HolidayOvertimeMultiplier is decimal m => (m, false),
        OvertimeDayType.Ordinary => (s.OrdinaryOvertimeMultiplier, false),
        _ => (s.OrdinaryOvertimeMultiplier, true)
    };

    public static string Label(OvertimeDayType t) => t switch
    {
        OvertimeDayType.RestDay => "Overtime (rest day)",
        OvertimeDayType.Holiday => "Overtime (holiday)",
        _ => "Overtime"
    };
}