namespace Core.Common;

public enum PayFrequency { SemiMonthly = 1, Monthly = 2}
public sealed record PayrollSettings(
    PayFrequency Frequency = PayFrequency.SemiMonthly,
    int WorkingDaysFactorFiveDay = 261,
    int WorkingDaysFactorSixDay = 313,
    decimal HoursPerDay = 8m,
    decimal OrdinaryOvertimeMultiplier = 1.25m);

public sealed record PayPeriod(DateOnly Start, DateOnly End)
{
    public int Days => End.DayNumber - Start.DayNumber + 1;
}

public sealed record GrossInput(
    IReadOnlyList<SalaryPoint> History,
    DateOnly HireDate,
    DateOnly? LastDay,
    PayPeriod Period,
    int WorkingDaysPerWeek,
    decimal UnpaindLeaveDays,
    int ApprovedOvertimeMinutes);

public sealed record GrossLine(string Code, string Label, decimal Amount);

public sealed record GrossResult(
    IReadOnlyList<GrossLine> Lines,
    decimal Gross,
    decimal MonthlyBasic,
    decimal DailyRate,
    decimal HourlyRate,
    IReadOnlyList<string> Notes,
    string? Error);