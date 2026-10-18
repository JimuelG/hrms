namespace Core.Common;

public sealed record SalaryPoint(DateOnly EffectiveForm, decimal MonthlyBasic);

public static class CompensationRules
{
    public const decimal MaxMonthly = 10_000_000m;
    public const int DaysAhead = 366;

    public static string? Check(decimal monthly, DateOnly effectiveFrom, DateOnly hireDate, DateOnly? latest, DateOnly today)
    {
        if (monthly <= 0 || monthly > MaxMonthly)
            return $"The monthly salary must be more than 0 and at most {MaxMonthly:N0}.";
        if (decimal.Round(monthly, 2) != monthly)
            return "The salary can have at most two decimal places.";
        if (effectiveFrom.Year < LeaveCalculator.MinYear || effectiveFrom.Year > LeaveCalculator.MaxYear)
            return "The effective date is out of range";
        if (effectiveFrom < hireDate)
            return $"The effective date can't be before the hire date ({hireDate:yyyy-MM-dd}).";
        if (effectiveFrom > today.AddDays(DaysAhead))
            return "The effective date can be at most a year ahead.";
        if (latest is DateOnly l && effectiveFrom <= l)
            return $"The effective date must be after the current entry's ({l:yyyy-MM-dd}). Earlier history can't be rewritten.";
        return null;
    }

    public static decimal? MonthlyOn(IEnumerable<SalaryPoint> history, DateOnly date) =>
        history.Where(p => p.EffectiveForm <= date)
            .OrderByDescending(p => p.EffectiveForm)
            .Select(p => (decimal?)p.MonthlyBasic)
            .FirstOrDefault();
}