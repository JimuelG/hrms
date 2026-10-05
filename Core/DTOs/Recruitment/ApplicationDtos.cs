using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs.Recruitment;

public sealed record ApplicationDto(
    Guid Id,
    Guid ApplicantId,
    string ApplicantName,
    string ApplicantEmail,
    Guid JobPostingId,
    string JobPostingTitle,
    int Status,
    string? Notes,
    DateTime AppliedAtUtc);


public sealed class CreateApplicationDto
{
    [Required]
    public Guid ApplicantId { get; init; }
    [Required]
    public Guid JobPostingId { get; init; }
    [MaxLength(2000)]
    public string? Notes { get; init; }
}

public sealed class UpdateApplicationStatusDto
{
    [Required]
    public int Status { get; init; }
    [MaxLength(2000)]
    public string? Notes { get; init; }
}
    