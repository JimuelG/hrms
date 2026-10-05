using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs.Recruitment;

public sealed record InterviewEvaluationDto(
    Guid Id,
    int Rating,
    int Recommendation,
    string? Strengths,
    string? Concerns,
    string? Notes,
    Guid SubmittedByUserId,
    DateTime SubmittedAtUtc);

public sealed record InterviewDto(
    Guid Id,
    Guid ApplicationId,
    string ApplicantName,
    string JobPostingTitle,
    int Type,
    DateTime ScheduledAtUtc,
    int DurationMinutes,
    string? Location,
    Guid InterviewId,
    string InterviewerName,
    int Status,
    string? CancellationReason,
    InterviewEvaluationDto? Evaluation);

public sealed class ScheduleInterviewDto
{
    [Required]
    public Guid ApplicationId { get; init; }
    [Required]
    public int Type { get; init; }
    [Required]
    public DateTime ScheduledAtUtc { get; init; }
    [Range(5, 480)]
    public int DurationMinutes { get; init; } = 30;
    [MaxLength(500)]
    public string? Location { get; init; }
    [Required]
    public Guid InterviewerId { get; init;}
}

public sealed class RescheduleInterviewDto
{
    [Required]
    public DateTime ScheduledAtUtc { get; init; }
    [Range(5, 480)]
    public int DurationMinutes { get; init; } = 30;
    [MaxLength(500)]
    public string? Location { get; init; }
}

public sealed class CancelInterviewDto
{
    [Required, MaxLength(500)]
    public string CancellationReason { get; init; } = "";
}

public sealed class SubmitEvaluationDto
{
    [Required, Range(1, 5)]
    public int Rating { get; init; }
    [Required]
    public int Recommendation { get; init; }
    [MaxLength(2000)]
    public string? Strengths { get; init; }
    [MaxLength(2000)]
    public string? Concerns { get; init; }
    [MaxLength(2000)]
    public string? Notes { get; init; }
}