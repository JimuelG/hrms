using System.Globalization;
using Core.Enums;

namespace Core.Common;

public enum LeaveDayStatus
{
    Counted = 1,
    NonWorkingDay = 2,
    Holiday = 3
}

public sealed record LeaveDayLine(
    DateOnly Date,
    decimal Value,
    LeaveDayStatus Status,
    string? HolidayName);

public sealed record LeaveCalculation(decimal Days, IReadOnlyList<LeaveDayLine> Lines, string? Error)
{
    public static LeaveCalculation Fail(string error) => new(0, [], error);
}
public static class LeaveCalculator
{
    public const int MaxRangeDays = 366;
    public const int MinYear = 2000;
    public const int MaxYear = 2100;

    public static LeaveCalculation Calculate(
        DateOnly start,
        DateOnly end,
        DayPortion startPortion,
        DayPortion endPortion,
        WorkingDay workingDays,
        IReadOnlyDictionary<DateOnly, string> holidays,
        bool allowsHalfDay)
    {
        if (!Enum.IsDefined(startPortion) || !Enum.IsDefined(endPortion)) return Fail("Invalid day portion");
        if (end < start) return Fail("The end date must be on or rafter the start date.");
        if (start.Year < MinYear || end.Year > MaxYear) return Fail($"Dates must be between {MinYear} and {MaxYear}.");
        if (end.DayNumber - start.DayNumber + 1 > MaxRangeDays)
            return Fail($"A single request can't cover more that {MaxRangeDays} days.");
        if (workingDays == WorkingDay.None)
            return Fail("No working days are defined for this employee's schedule. Contact HR.");

        if (!allowsHalfDay && (startPortion != DayPortion.Full || endPortion != DayPortion.Full))
            return Fail("This leave type doesn't allow half-days.");

        var singleDay = start == end;
        if (singleDay && startPortion != endPortion)
            return Fail("For a single day, the start and end portions must match.");
        if (!singleDay && startPortion == DayPortion.Morning)
            return Fail("On the first day of a multi-day leave you can take the whole day or the afternoon only.");
        if (!singleDay && endPortion == DayPortion.Afternoon)
            return Fail("On the last day of a multi-day leave you can take the whole day or the morning only.");

        var lines = new List<LeaveDayLine>();
        decimal total = 0;

        for (var d = start; d <= end; d = d.AddDays(1))
        {
            var portion = d == start ? startPortion : d == end ? endPortion : DayPortion.Full;

            LeaveDayLine line;
            if (!workingDays.HasFlag(d.DayOfWeek.ToWorkingDay()))
                line = new(d, 0, LeaveDayStatus.NonWorkingDay, null);
            else if (holidays.TryGetValue(d, out var holidayName))
                line = new(d, 0, LeaveDayStatus.Holiday, holidayName);
            else
                line = new(d, portion == DayPortion.Full ? 1m : 0.5m, LeaveDayStatus.Counted, null);

            if (portion != DayPortion.Full && line.Status != LeaveDayStatus.Counted)
                return Fail($"{d.ToString("MMM d", CultureInfo.InvariantCulture)} is " +
                $"{(line.Status == LeaveDayStatus.Holiday ? "a holiday" : "not a working day")}, " +
                "so a half-day doesn't apply there.");

            lines.Add(line);
            total += line.Value;
        }

        if (total == 0)
            return Fail("That range only covers days off and holidays, so there's no leave to take.");

        return new LeaveCalculation(total, lines, null);
    }

    public static bool RequiresDocument(decimal days, int? afterDays) => afterDays is int n && days > n;
    private static LeaveCalculation Fail(string error) => LeaveCalculation.Fail(error);
}