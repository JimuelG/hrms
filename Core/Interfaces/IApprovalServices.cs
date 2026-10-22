using Core.Common;
using Core.DTOs.Approvals;
using Core.Entities;
using Core.Enums;

namespace Core.Interfaces;

public sealed record ApprovalSubjectContext(
    Guid ApprovalRequestId,
    ApprovalSubjectType SubjectType,
    Guid SubjectId,
    Guid RequesterEmployeeId,
    string Title,
    Guid ActorUserId);

public interface IApprovalSubjectHandler
{
    ApprovalSubjectType SubjectType { get; }
    Task<string?> OnApprovedAsync(ApprovalSubjectContext context, CancellationToken ct = default);
    Task OnRejectedAsync(ApprovalSubjectContext context, CancellationToken ct = default);
    Task OnCancelledAsync(ApprovalSubjectContext context, CancellationToken ct = default);
}

public interface IApprovalQueryService
{
    Task<ServiceResult<ApprovalRequestDetailDto>> GetAsync(Guid userId, Guid requestId, CancellationToken ct = default);
    Task<ServiceResult<ApprovalRequestDetailDto>> GetBySubjectAsync(Guid userId, ApprovalSubjectType subjectType, Guid subjectId, CancellationToken ct = default);

    Task<ServiceResult<PagedResult<ApprovalRequestSummaryDto>>> GetInboxAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<ServiceResult<PagedResult<ApprovalRequestSummaryDto>>> GetMineAsync(Guid userId, ApprovalStatus? status, int page, int pageSize, CancellationToken ct = default);

    Task<ApprovalRequestDetailDto> DescribeAsync(ApprovalRequest request, Guid viewerUserId, CancellationToken ct = default);
}

public interface IApprovalWorkflowService
{
    Task<ServiceResult<SubmissionResult>> StageSubmissionAsync(ApprovalSubmission submission, CancellationToken ct = default);

    Task<ServiceResult<ApprovalRequestDetailDto>> ApprovedAsync(Guid actorUserId, Guid requestId, string? note, CancellationToken ct = default);
    Task<ServiceResult<ApprovalRequestDetailDto>> RejectAsync(Guid actorUserId, Guid requestId, string? note, CancellationToken ct = default);
    Task<ServiceResult<ApprovalRequestDetailDto>> CommentAsync(Guid actorUserId, Guid requestId, string? note, CancellationToken ct = default);

    Task<ServiceResult<ApprovalRequestDetailDto>> CancelAsync(Guid actorUserId, ApprovalSubjectType subjectType, Guid subjectId, string? note, CancellationToken ct = default);
}