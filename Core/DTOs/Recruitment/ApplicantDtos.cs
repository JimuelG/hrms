using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Recruitment;
public sealed record ApplicantDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? SkillsSummary,
    string? EducationSummary,
    string? ExperienceSummary,
    string? ResumeOriginalFileName,
    DateTime CreatedAtUtc);

public class CreateApplicantDto
{
    [Required, MaxLength(100)]
    public string FirstName { get; init; } = "";
    [Required, MaxLength(100)]
    public string LastName { get; init; } = "";
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = "";
    [MaxLength(30)]
    public string? Phone { get; init; }
    [MaxLength(1000)]
    public string? SkillsSumarry { get; init; }
    [MaxLength(1000)]
    public string? EducationSummary { get; init; }
    [MaxLength(1000)]
    public string? ExperienceSummary { get; init; }
}

public class UpdateApplicantDto : CreateApplicantDto {}