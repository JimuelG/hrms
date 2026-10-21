namespace Core.DTOs.Payroll;
public sealed record PayslipSummaryDto(
    Guid RunId, 
    DateOnly PeriodStart, 
    DateOnly PeriodEnd,
    decimal Net,
    DateTime? FinalizedAtUtc,
    int Kind);

public sealed record PayslipDto(
    Guid RunId,
    int RunStatus,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateTime? FinalizedAtUtc,
    string? CompanyName,
    string EmployeeNumber,
    string EmployeeName,
    string? PositionTitle,
    string? DepartmentName,
    decimal MonthlyBasic,
    decimal Gross,
    decimal TaxableIncome,
    decimal TotalEarnings,
    decimal TotalDeductions,
    decimal Net,
    IReadOnlyList<PayrollLineDto> Lines,
    IReadOnlyList<string> Notes,
    PayslipYtdDto Ytd,
    int Kind);

public sealed record PayslipYtdDto(
    decimal Gros,
    decimal TaxableIncome,
    decimal TaxWithheld,
    decimal Net);