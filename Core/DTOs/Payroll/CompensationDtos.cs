using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Payroll;
public sealed record CompensationDto(
    Guid Id,
    Guid EmployeeId,
    DateOnly EffectiveFrom,
    decimal MonthlyBasicSalary,
    string Reason,
    string SetByName,
    DateTime CreatedAtUtc,
    bool IsCurrent);

public sealed class SetCompensationDto
{
    [Range(0.01, 10000000)]
    public decimal MonthlyBasicSalary { get; init; }
    [Required]
    public DateOnly? EffectiveFrom { get; init; }
    [Required, MaxLength(500)]
    public string Reason { get; init; } = "";
}