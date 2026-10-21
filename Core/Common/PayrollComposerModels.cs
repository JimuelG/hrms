namespace Core.Common;
public enum PayrollLineKind { Earning = 1, Deduction = 2 }

public sealed record PayrollLine(string Code, string Label, decimal Amount, PayrollLineKind Kind);

public sealed record PayrollAdjustment(string Label, decimal Amount, AdjustmentTax Tax = AdjustmentTax.NonTaxable);
public sealed record PayrollEmployeeInput(GrossInput Gross, IReadOnlyList<PayrollAdjustment> Adjustments, YearToDate? Ytd = null);
public sealed record PayrollOptions(PayrollSettings Settings, ContributionDeduction ContributionPolicy);

public sealed record PayrollResult(
    IReadOnlyList<PayrollLine> Lines,
    decimal Gross,
    decimal TaxableIncome,
    decimal TotalEarnings,
    decimal TotalDeductions,
    decimal Net,
    decimal EmployerContributions,
    decimal MonthlyBasic,
    decimal DailyRate,
    decimal HourlyRate,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> Warnings,
    string? Error);