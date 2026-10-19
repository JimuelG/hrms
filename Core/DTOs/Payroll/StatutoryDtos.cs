using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Payroll;

public sealed record BracketDto(
    int Order,
    decimal From,
    decimal? To,
    decimal EmployeeFixed,
    decimal EmployerFixed,
    decimal EmployeeRateOnExcess);

public sealed record RuleSetDto(
    Guid Id,
    int Kind,
    int Method,
    DateOnly EffectiveFrom,
    int? Basis,
    decimal EmployeeRate,
    decimal EmployerRate,
    decimal? BaseFloor,
    decimal? BaseCeiling,
    string Source,
    string? Notes,
    string SetByName,
    DateTime CreatedAtUtc,
    bool IsInForce,
    IReadOnlyList<BracketDto> Brackets);

public sealed class BracketInput
{
    public decimal From { get; init; }
    public decimal? To { get; init; }
    public decimal EmployeeFixed { get; init; }
    public decimal EmployerFixed { get; init; }
    public decimal EmployeeRateOnExcess { get; init; }
}

public sealed class CreateRuleSetDto
{
    public int Kind { get; init; }
    public int Method { get; init; }
    [Required]
    public DateOnly? EffectiveFrom { get; init; }
    public int? Basis { get; init; }

    public decimal EmployeeRate { get; init; }
    public decimal EmployerRate { get; init; }
    public decimal? BaseFloor { get; init; }
    public decimal? BaseCeiling { get; init; }

    [Required, MaxLength(300)]
    public string Source { get; init; } = "";
    [MaxLength(1000)]
    public string? Notes { get; init; }

    public List<BracketInput>? Brackets { get; init; }
    public string? BracketsCsv { get; init; }
}

public sealed class StatutoryPreviewQuery
{
    [Required, Range(0, 10000000)]
    public decimal? MonthlyBase { get; set; }
    [Range(0, 10000000)]
    public decimal? TaxableIncome { get; set; }
    public int? TaxBasis { get; set; }
    public DateOnly? Date { get; set; }
}

public sealed record StatutoryLineDto(
    string Code,
    string Label,
    decimal Employee,
    decimal Employer,
    DateOnly? RuleEffectiveFrom,
    string? Note);

public sealed record StatutoryPreviewDto(
    DateOnly Date,
    IReadOnlyList<StatutoryLineDto> Lines);