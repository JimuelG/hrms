using System.ComponentModel.DataAnnotations;
using Core.Entities;
using Core.Enums;

namespace Core.DTOs.Approvals;
public sealed record ApprovalSubmission(
    ApprovalSubjectType SubjectType,
    Guid SubjectId,
    Guid RequesterEmployeeId,
    string Title,
    string? Details,
    Guid? ActorUserId);

public sealed record SubmissionResult(Guid ApprovalRequestId, string? PendingStepLabel, IReadOnlyList<string> SkipNotes);

public sealed record ApprovalRequestSummaryDto(
    Guid Id,
    int SubjectType,
    Guid SubjectId,
    Guid RequesterEmployeeId,
    string RequesterName,
    string REquesterNumber,
    string Title,
    string? Details,
    int Status,
    string? CurrentStepLabel,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc);

public sealed record ApprovalStepDto(
    int Order,
    int Kind,
    string Label,
    int Status,
    string? ApproverName,
    string? SkipReason,
    string? DecidedByName,
    DateTime? DecidedAtUtc,
    string? Note);

public sealed record ApprovalEventDto(
    int Type,
    int? StepOrder,
    string? ActorName,
    string? Note,
    DateTime CreatedAtUtc);

public sealed record ApprovalAccessDto(bool IsRequester, bool CanDecide, bool CanComment, bool CanCancel);

public sealed record ApprovalRequestDetailDto(
    ApprovalRequestSummaryDto Request,
    IReadOnlyList<ApprovalStepDto> Steps,
    IReadOnlyList<ApprovalEventDto> Events,
    ApprovalAccessDto Access);

public sealed class ApprovalNoteDto
{
    [MaxLength(500)]
    public string? Note { get; init; }
}