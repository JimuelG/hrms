namespace Core.Common;

public enum PayFrequency { SemiMonthly = 1, Monthly = 2}
public enum PartialMonthContribution { FullPeriod = 1, ByDaysEmployed = 2, SkipIfPartial = 3 }
public enum OvertimeDayType { Ordinary = 1, RestDay = 2, Holiday = 3 }
public enum AdjustmentTax { NonTaxable = 1, Taxable = 2 }
public enum TaxMethod { PerPeriod = 1, Cumulative =2 }

public sealed record OvertimeBucket(OvertimeDayType DayType, int Minutes);
public sealed record PayrollSettings(
    PayFrequency Frequency = PayFrequency.SemiMonthly,
    int WorkingDaysFactorFiveDay = 261,
    int WorkingDaysFactorSixDay = 313,
    decimal HoursPerDay = 8m,
    decimal OrdinaryOvertimeMultiplier = 1.25m,
    decimal? RestDayOvertimeMultiplier = null,
    decimal? HolidayOvertimeMultiplier = null,
    bool AttendanceDeductionsEnabled = false,
    int AttendanceGraceMinutes = 0,
    PartialMonthContribution PartialMonth = PartialMonthContribution.FullPeriod,
    TaxMethod TaxMethod = TaxMethod.PerPeriod);

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
    decimal UnpaidLeaveDays,
    IReadOnlyList<OvertimeBucket> Overtime,
    AttendanceDeductionInput? Attendance = null);

public sealed record GrossLine(string Code, string Label, decimal Amount);

public sealed record GrossResult(
    IReadOnlyList<GrossLine> Lines,
    decimal Gross,
    decimal MonthlyBasic,
    decimal DailyRate,
    decimal HourlyRate,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> Warnings,
    string? Error);

public static class OvertimeBuckets
{
    public static IReadOnlyList<OvertimeBucket> Ordinary(int minutes) =>
        minutes > 0 ? [new(OvertimeDayType.Ordinary, minutes)] : [];
}

public sealed record AttendanceDay(DateOnly Date, int LateMinutes, int UndertimeMinutes, bool OpenRecord);

public sealed record AttendanceDeductionInput(
    bool Enabled,
    int GraceMinutes,
    int AbsentDays,
    int LateMinutes,
    int UndertimeMinutes,
    IReadOnlyList<string> Flags);

public sealed record YearToDate(decimal TaxableIncome, decimal TaxWithheld);