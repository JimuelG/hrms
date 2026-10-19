namespace Core.Common;
public static class GrossPayCalculator
{
    public static GrossResult Calculate(GrossInput i, PayrollSettings s)
    {
        if (i.WorkingDaysPerWeek is not (5 or 6))
            return Fail("Only five-day and six-day work weeks are supported.");
        if (i.UnpaindLeaveDays < 0 || i.ApprovedOvertimeMinutes < 0)
            return Fail("Leave days and overtime can't be negative.");

        var notes = new List<string>();

        var effectiveDate = i.HireDate > i.Period.Start ? i.HireDate : i.Period.Start;
        var monthly = CompensationRules.MonthlyOn(i.History, effectiveDate);
        if (monthly is null) return Fail("No salary is on record for this employee on that date.");

        var changes = i.History.Count(p => p.EffectiveForm > effectiveDate && p.EffectiveForm <= i.Period.End);
        if (changes > 0)
            notes.Add("A salary change takes effect during this period. This pass pays the whole period at the earlier rate; adjust manually.");

        var factor = i.WorkingDaysPerWeek == 5 ? s.WorkingDaysFactorFiveDay : s.WorkingDaysFactorSixDay;
        var daily = Money(monthly.Value * 12m / factor);
        var hourly = Money(daily / s.HoursPerDay);

        var fraction = PayPeriodCalculator.EmployedFraction(i.Period, i.HireDate, i.LastDay);
        if (fraction == 0m) return Fail("The employee wasn't employed during this period.");

        var periodsPerMonth = s.Frequency == PayFrequency.SemiMonthly ? 2m : 1m;
        var lines = new List<GrossLine>
        {
            new("BASIC", "Basic salary", Money(monthly.Value / periodsPerMonth * fraction))
        };
        if (fraction < 1m) notes.Add($"Prorated: employed {fraction:P0} of this period.");

        if (i.UnpaindLeaveDays > 0)
            lines.Add(new("UNPAID_LEAVE", "Unpaid leave", -Money(daily * i.UnpaindLeaveDays)));

        if (i.ApprovedOvertimeMinutes > 0)
        {
            var hours = i.ApprovedOvertimeMinutes / 60m;
            lines.Add(new("OVERTIME", "Overtime", Money(hourly * s.OrdinaryOvertimeMultiplier * hours)));
            notes.Add("Overtime is paid at the ordinary-day rate. Rest-day and holiday premiums aren't applied.");
        }

        var gross = lines.Sum(l => l.Amount);
        return new GrossResult(lines, gross, monthly.Value, daily, hourly, notes, null);
    }

    private static decimal Money(decimal v) => decimal.Round(v, 2, MidpointRounding.AwayFromZero);

    private static GrossResult Fail(string error) => new([], 0m, 0m, 0m, 0m, [], error);
}