using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs.Onboarding;

public sealed record OnboardingTaskDto(
    Guid Id,
    string Title,
    string? Description,
    bool IsRequired,
    bool IsCompleted,
    DateTime? CompletedAtUtc);
public sealed record OnboardingCaseDto(
    Guid Id,
    Guid ApplicationId,
    string ApplicantName,
    string JobPostingTitle,
    int Status,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    Guid? CreatedEmployeeId,
    IReadOnlyList<OnboardingTaskDto> Tasks);

public sealed class StartOnboardingDto
{
    [Required]
    public Guid ApplicationId { get; init; }
}

public sealed class ConvertToEmloyeeDto
{
    [Required, MaxLength(30)]
    public string EmployeeNumber { get; init; } = "";
    [Required]
    public Guid BranchId { get; init; }
    [Required]
    public Guid DepartmentId { get; init; }
    [Required]
    public Guid PositionId { get; init; }
    public Guid? ManagerId { get; set; }
    [Required]
    public EmploymentType EmploymentType { get; init; }
    [Required]
    public DateOnly HireDate { get; init; }
}