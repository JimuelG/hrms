using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs.Recruitment;
public sealed record JobOfferDto(
    Guid Id,
    Guid ApplicationId,
    string ApplicantName,
    string JobPostingTitle,
    decimal ProposedSalary,
    string Currency,
    DateOnly ProposedStartDate,
    DateOnly? ExpiresOn,
    string? Terms,
    int Status,
    DateTime? SentAtUtc,
    DateTime? RespondedAtUtc,
    string? DeclineReason,
    DateTime CreatedAtUtc);

public sealed class CreateJobOfferDto
{
    [Required]
    public Guid ApplicationId { get; init; }
    [Required, Range(0, double.MaxValue)]
    public decimal ProposedSalary { get; init; }
    [Required, MaxLength(3)]
    public string Currency { get; init; } = "PHP";
    [Required]
    public DateOnly ProposedStartDate { get; init; }
    public DateOnly? ExpiresOn { get; init; }
    [MaxLength(2000)]
    public string? Terms { get; init; }
}

public sealed class DeclineJobOfferDto
{
    [MaxLength(500)]
    public string? DeclineReason { get; init; }
}