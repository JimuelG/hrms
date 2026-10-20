namespace Core.Common;
public static class GrossPayCalculator
{
    public static GrossResult Calculate(GrossInput i, PayrollSettings s)
    {
        if (i.WorkingDaysPerWeek is not (5 or 6))
            return Fail("Only five-day and six-day work weeks are supported.");
        if (i.UnpaidLeaveDays < 0 || i.Overtime.Any(b => b.Minutes < 0))
            return Fail("Leave days and overtime can't be negative.");

        var window = PayPeriodCalculator.EmployedWindow(i.Period, i.HireDate, i.LastDay);
        if (window is null) return Fail("The employee wasn't employed during this period.");

        var segments = SalarySegmenter.Split(i.History, window.Value.Start, window.Value.End);
        if (segments.Count == 0) return Fail("No salary is on record for this employee on that date.");

        var notes = new List<string>();

        var monthly = segments[^1].Monthly;
        var factor = i.WorkingDaysPerWeek == 5 ? s.WorkingDaysFactorFiveDay : s.WorkingDaysFactorSixDay;
        var daily = Money(monthly * 12m / factor);
        var hourly = Money(daily / s.HoursPerDay);

        var periodsPerMonth = s.Frequency == PayFrequency.SemiMonthly ? 2m : 1m;
        var basic = segments.Sum(g => g.Monthly / periodsPerMonth * g.Days / i.Period.Days);
        var lines = new List<GrossLine> { new("BASIC", "Basic salary", Money(basic)) };

        var employedDays = window.Value.End.DayNumber - window.Value.Start.DayNumber + 1;
        if (employedDays < i.Period.Days)
            notes.Add($"Prorated: employed {(decimal)employedDays / i.Period.Days:P0} of this period.");
        if (segments.Count > 1)
            notes.Add("Salary changed during this period. Each part was paid by calendar days at the rate then in force. " + "Leave and overtime use the latest rate.");

        if (i.UnpaidLeaveDays > 0)
            lines.Add(new("UNPAID_LEAVE", "Unpaid leave", -Money(daily * i.UnpaidLeaveDays)));

        var warnings = new List<string>();

        if (i.Attendance is { Enabled: true } a)
        {
            var absent = daily * a.AbsentDays;
            var late = hourly * a.LateMinutes / 60m;
            var under = hourly * a.UndertimeMinutes / 60m;
            var basicLine = lines[0].Amount;

            var remaining = basicLine;
            foreach (var (code, label, raw) in new []
            {
                ("ABSENT", "Absences", absent),
                ("LATE", "Late", late),
                ("UNDERTIME", "Undertime", under)
            })
            {
                var amount = AttendanceDeductionRules.Capped(Money(raw), remaining);
                if (amount > 0m)
                {
                    lines.Add(new(code, label, -amount));
                    remaining -= amount;
                }
            }

            if (absent + late + under > basicLine)
                warnings.Add("Attendance deductions were capped at the basic salary for this period.");

            foreach (var f in a.Flags)
            {
                warnings.Add(f);
            }
        }

        foreach (var bucket in i.Overtime.Where(b => b.Minutes > 0).OrderBy(b => b.DayType))
        {
            var (multiplier, fellBack) = OvertimeClassifier.MultiplierFor(bucket.DayType, s);
            var hours = bucket.Minutes / 60m;
            lines.Add(new("OVERTIME", OvertimeClassifier.Label(bucket.DayType), Money(hourly * multiplier * hours)));

            if (fellBack)
                warnings.Add($"{bucket.Minutes} minutes of {(bucket.DayType == OvertimeDayType.RestDay ? "rest-day" : "holiday" )} " +
                            "overtime were paid at the ordinary rate because no premium is set in payroll settings.");
        }

        var gross = lines.Sum(l => l.Amount);
        return new GrossResult(lines, gross, monthly, daily, hourly, notes, warnings, null);
    }

    private static decimal Money(decimal v) => decimal.Round(v, 2, MidpointRounding.AwayFromZero);

    private static GrossResult Fail(string error) => new([], 0m, 0m, 0m, 0m, [], [], error);
}