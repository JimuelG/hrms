using Core.Enums;

namespace Core.Common;
public static class LeaveOverlap
{
    public static HashSet<(DateOnly Date, int Half)> Slots(DateOnly start, DateOnly end, DayPortion startPortion, DayPortion endPortion, IEnumerable<DateOnly> countedDates)
    {
        var slots = new HashSet<(DateOnly, int)>();
        var single = start == end;

        foreach (var date in countedDates.Distinct())
        {
            if (date < start || date > end) continue;

            var portion = single || date == start ? startPortion
                : date == end ? endPortion
                : DayPortion.Full;

            if (portion != DayPortion.Afternoon) slots.Add((date, 0));
            if (portion != DayPortion.Morning) slots.Add((date, 1));
        }

        return slots;
    }

    public static bool Overlaps(IReadOnlySet<(DateOnly Date, int Half)> a, IReadOnlySet<(DateOnly Date, int Half)> b) => a.Overlaps(b);
}