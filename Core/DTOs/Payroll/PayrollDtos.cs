using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Payroll;
public sealed record PayrollLineDto(
    string Code,
    string Label,
    decimal Amount,
    int Kind);

public sealed record PayrollResultDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeNumber,
    string EmployeeName,
    string? DepartmentName,
    string? PositionTitle,
    decimal MonthlyBasic,
    decimal Gross,
    decimal TaxableIncome,
    decimal TotalEarnings,
    decimal TotalDeductions,
    decimal Net,
    decimal EmployerContributions,
    IReadOnlyList<PayrollLineDto> Lines,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> Warnings,
    string? ErrorMessage);

public sealed record PayrollRunSummaryDto(
    Guid Id,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int Status,
    int EmployeeCount,
    int ErrorCount,
    decimal TotalGross,
    decimal TotalDeductions,
    decimal TotalNet,
    decimal TotalEmployerContributions,
    DateTime CreatedAtUtc,
    DateTime? FinalizedAtUtc);

public sealed record PayrollRunDetailDto(PayrollRunSummaryDto Run, IReadOnlyList<PayrollResultDto> Results);

public sealed class StartRunDto
{
    [Required]
    public DateOnly? PeriodStart { get; init; }
    [Required]
    public DateOnly? PeriodEnd { get; init; }
    [MaxLength(1000)]
    public string? Notes { get; init; }
}

public sealed class AddAdjusmentDto
{
    [Required]
    public Guid? EmployeeId { get; init; }
    [Required, MaxLength(100)]
    public string Label { get; init; } = "";
    public decimal Amount { get; init; }
}

public sealed class ReasonDto
{
    [MaxLength(500)]
    public string? Reason { get; init; }
}

public sealed record PayrollAdjustmentDto(Guid Id, Guid EmployeeId, string Label, decimal Amount);

public sealed record PayrollSettingsDto(
    int Frequency,
    int ContributionPolicy,
    int WorkingDaysFactorFiveDay,
    int WorkingDaysFactorSixDay,
    decimal HoursPerDay,
    decimal OrdinaryOvertimeMultiplier);

public sealed class UpdatePayrollSettingsDto
{
    public int Frequency { get; init; }
    public int ContributionPolicy { get; init; }
    [Range(200, 366)]
    public int WorkingDaysFactorFiveDay { get; init; }
    [Range(200, 366)]
    public int WorkingDaysFactorSixDay { get; init; }
    [Range(1, 24)]
    public decimal HoursPerDay { get; init; }
    [Range(1, 5)]
    public decimal OrdinaryOvertimeMultiplier { get; init; }
}