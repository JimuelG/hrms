using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Recruitment;

public sealed record JobPostingDto(
    Guid Id,
    string Title,
    Guid PositionId,
    string PositionTitle,
    Guid DepartmentId,
    string DepartmentName,
    Guid BranchId,
    string BranchName,
    int EmploymentType,
    string Description,
    string? Requirements,
    string? Skills,
    string? EducationRequirement,
    string? ExperienceRequirement,
    decimal? SalaryMin,
    decimal? SalaryMax,
    bool SalaryVisible,
    int Vacancies,
    DateOnly? ApplicationDeadline,
    Guid? HiringManagerId,
    string? HiringManagerName,
    int Status,
    DateTime CreatedAtUtc);

public class CreateJobPostingDto
{
    [Required, MaxLength(200)]
    public string Title { get; init; } = "";
    [Required]
    public Guid PositionId { get; init; }
    [Required]
    public Guid DepartmentId { get; init; }
    [Required]
    public Guid BranchId { get; init; }
    [Required]
    public int EmploymentType { get; init; }

    [Required, MaxLength(4000)]
    public string Description { get; init; } = "";
    [MaxLength(4000)]
    public string? Requirements { get; init; }
    [MaxLength(1000)]
    public string? Skills { get; init; }
    [MaxLength(300)]
    public string? EducationRequirement { get; init; }
    [MaxLength(300)]
    public string? ExperienceRequirement { get; init; }

    [Range(0, double.MaxValue)]
    public decimal? SalaryMin { get; init; }
    [Range(0, double.MaxValue)]
    public decimal? SalaryMax { get; init; }
    public bool SalaryVisible { get; init; }

    [Range(1, 1000)]
    public int Vacancies { get; init; } = 1;
    public DateOnly? ApplicationDeadline { get; init; }
    public Guid? HiringManagerId { get; init; }
}

public class UpdateJobPostingDto : CreateJobPostingDto
{
    [Required]
    public int Status { get; init; }
}