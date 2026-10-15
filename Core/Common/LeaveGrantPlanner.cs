using Core.Enums;

namespace Core.Common;

public sealed record LeavePolicy(
    decimal DefaultDays, 
    bool ProrateFirstYear, 
    decimal MaxCarryOverDays, 
    int MinServiceMonths);

public sealed record LedgerRow(
    int Year,
    LeaveEntryType Type,
    decimal Days,
    bool IsAutomatic);

public sealed record PlannedEntry(
    int Year,
    LeaveEntryType Type,
    decimal Days,
    string Description);

public static class LeaveGrantPlanner
{
    public static DateOnly EligibleFrom(DateOnly hireDate, int minServiceMonths) => hireDate.AddMonths(minServiceMonths);

    public static decimal Prorate(decimal fullDays, DateOnly basis, int year)
    {
        var first = new DateOnly(year, 1, 1);
        var last = new DateOnly(year, 12, 31);

        if (basis <= first) return fullDays;
        if (basis > last) return 0;

        var daysInYear = last.DayNumber - first.DayNumber + 1;
        var remaining = last.DayNumber - basis.DayNumber + 1;

        var raw = fullDays * remaining / daysInYear;
        var rounded = Math.Round(raw * 2, MidpointRounding.AwayFromZero) / 2;
        return Math.Min(rounded, fullDays);
    }

    public static IReadOnlyList<PlannedEntry> Plan(
        LeavePolicy policy, DateOnly hireDate, DateOnly asOf, IReadOnlyList<LedgerRow> existing)
    {
        var eligibleFrom = EligibleFrom(hireDate, policy.MinServiceMonths);
        if (eligibleFrom > asOf) return [];

        var grantedYears = existing.Where(r => r.IsAutomatic && r.Type == LeaveEntryType.Grant).Select(r => r.Year).ToList();
        var startYear = grantedYears.Count > 0 ? grantedYears.Max() + 1 : asOf.Year;

        var running = existing.GroupBy(r => r.Year).ToDictionary(g => g.Key, g => g.Sum(r => r.Days));
        var planned = new List<PlannedEntry>();

        for (var year = startYear; year <= asOf.Year; year++)
        {
            if (eligibleFrom.Year > year) continue;

            var firstYear = year == eligibleFrom.Year && policy.ProrateFirstYear;
            var grant = firstYear ? Prorate(policy.DefaultDays, eligibleFrom, year) : policy.DefaultDays;

            planned.Add(new PlannedEntry(year, LeaveEntryType.Grant, grant,
                firstYear && grant != policy.DefaultDays
                    ? $"Annual entitlement for {year}, prorated from {eligibleFrom:yyyy-MM-dd}"
                    : $"Annual entitlement for {year}"));

            var carry = 0m;
            if (policy.MaxCarryOverDays > 0 && running.TryGetValue(year - 1, out var previous) && previous > 0)
            {
                carry = Math.Min(previous, policy.MaxCarryOverDays);
                planned.Add(new PlannedEntry(year, LeaveEntryType.CarryOver, carry, $"Unused days carrried over from {year -1}"));
            }

            running[year] = running.GetValueOrDefault(year) + grant + carry;
        }

        return planned;
    }
}