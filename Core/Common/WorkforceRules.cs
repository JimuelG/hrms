using System.Reflection.Emit;
using System.Text;

namespace Core.Common;

public sealed record CountRow(string Label, int Count);

public static class WorkforceRules
{
    public const string Unassigned = "Unassigned";

    public static IReadOnlyList<CountRow> CountBy(IEnumerable<string?> labels) =>
        labels.GroupBy(l => string.IsNullOrWhiteSpace(l) ? Unassigned : l.Trim())
            .Select(g => new CountRow(g.Key, g.Count()))
            .OrderByDescending(r => r.Count).ThenBy(r => r.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static readonly string[] TenureBands = ["Under 1 year", "1 to under 3 years", "3 to under 5 years", "5 to under 10 years", "10 years or more"];

    public static int WholeYears(DateOnly from, DateOnly to)
    {
        if (to < from) return 0;
        var years = to.Year - from.Year;
        if (MonthDay(to) < MonthDay(from)) years--;
        return Math.Max(0, years);
    }

    public static string TenureBand(DateOnly hire, DateOnly today) => WholeYears(hire, today) switch
    {
        < 1 => TenureBands[0],
        < 3 => TenureBands[1],
        < 5 => TenureBands[2],
        < 10 => TenureBands[3],
        _ => TenureBands[4]
    };

    public static IReadOnlyList<CountRow> TenureDistribution(IEnumerable<DateOnly> hires, DateOnly today)
    {
        var counts = hires.GroupBy(h => TenureBand(h, today)).ToDictionary(g => g.Key, g => g.Count());
        return TenureBands.Select(b => new CountRow(b, counts.GetValueOrDefault(b))).ToList();
    }

    private static int MonthDay(DateOnly d) => d.Month * 100 + d.Day;

    public static DateOnly NextOccurrence(int month, int day, DateOnly today)
    {
        DateOnly In(int year) =>
            month == 2 && day == 29 && !DateTime.IsLeapYear(year) ? new DateOnly(year, 2, 28) : new DateOnly(year, month, day);

        var thisYear = In(today.Year);
        return thisYear >= today ? thisYear : In(today.Year + 1);
    }

    public static bool IsWithin(DateOnly date, DateOnly today, int days) => date >= today && date <= today.AddDays(days);

    public static string CsvCell(string? value)
    {
        var s = value ?? "";
        if (s.Length > 0 && s[0] is '=' or '+' or '-' or '@' or '\t' or '\r') s = "'" + s;
        return s.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
    }

    public static string ToCsv(string[] headers, IEnumerable<string?[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(CsvCell)));
        foreach (var row in rows) sb.AppendLine(string.Join(",", row.Select(CsvCell)));
        return sb.ToString();
    }
}