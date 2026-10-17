using Core.Common;
using Core.Constant;
using Core.DTOs.Leave;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Features.Leave;

public sealed class LeaveAttachmentService(
    IUnitOfWork unit,
    IGenericRepository<LeaveRequestAttachment> attachments,
    IGenericRepository<LeaveRequest> requests,
    IGenericRepository<Employee> employees,
    IGenericRepository<ApprovalRequest> approvals,
    IFileStorageService storage,
    IUserDirectory users,
    IPermissionService permissions,
    ITenantContext tenant) : ILeaveAttachmentService
{
    private const string NotFound = "Leave request not found.";
    private sealed record Access(LeaveRequest Request, bool IsOwner, bool IsHr);

    private async Task<Access?> AuthorizeAysnc(Guid userId, Guid requestId, CancellationToken ct)
    {
        var request = await requests.GetByIdAsync(requestId, ct);
        if (request is null) return null;

        var employee = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        var isOwner = employee?.Id == request.EmployeeId;

        var codes = (await permissions.GetPermissionsAsync(userId, tenant.RequiredTenantId, ct)).ToHashSet();
        var isHr = codes.Contains(Permissions.Leave.Manage);

        if (!isOwner && !isHr)
        {
            var approval = (await approvals.ListAsync(new ApprovalRequestBySubjectSpecification(ApprovalSubjectType.LeaveRequest, request.Id), ct)).FirstOrDefault();
            if (approval is null) return null;

            var viewer = new ApprovalAccessRules.Viewer(userId, employee?.Id, codes);
            var states = approval.Steps.Select(s => new StepState(s.Order, s.Kind, s.Label, s.Permission, s.ApproverEmployeeId, s.Status, s.SkipReason)).ToList();
            if (!ApprovalAccessRules.Evaluate(viewer, approval.RequesterEmployeeId, approval.Status, states).CanSeeRequest) return null;
        }

        return new Access(request, isOwner, isHr);
    }
    public async Task<ServiceResult<bool>> DeleteAsync(Guid userId, Guid requestId, Guid attachmentId, CancellationToken ct = default)
    {
        var access = await AuthorizeAysnc(userId, requestId, ct);
        if (access is null) return ServiceResult<bool>.Fail(NotFound, ServiceErrorType.NotFound);

        if (!access.IsOwner) return ServiceResult<bool>.Fail("Only the person who made the remove documents.");
        if (access.Request.Status != ApprovalStatus.Pending)
            return ServiceResult<bool>.Fail("Documents can only be removed while the request is pending.", ServiceErrorType.Conflict);

        var row = await attachments.GetByIdAsync(attachmentId, ct);
        if (row is null || row.LeaveRequestId != requestId)
            return ServiceResult<bool>.Fail("Document not found.", ServiceErrorType.NotFound);

        attachments.Remove(row);
        await unit.Complete();
        await storage.DeleteAsync(row.StorageKey, ct);
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<LeaveAttachmentFile>> DownloadAsync(Guid userId, Guid requestId, Guid attachmentId, CancellationToken ct = default)
    {
        var access = await AuthorizeAysnc(userId, requestId, ct);
        if (access is null) return ServiceResult<LeaveAttachmentFile>.Fail(NotFound, ServiceErrorType.NotFound);

        var row = await attachments.GetByIdAsync(attachmentId, ct);
        if (row is null || row.LeaveRequestId != requestId)
            return ServiceResult<LeaveAttachmentFile>.Fail("Document not found.", ServiceErrorType.NotFound);

        var file = await storage.GetAsync(row.StorageKey, ct);
        if (file is null) return ServiceResult<LeaveAttachmentFile>.Fail("The file is no longer available. Ask the send to updated it again,", ServiceErrorType.NotFound);

        return ServiceResult<LeaveAttachmentFile>.Success(new LeaveAttachmentFile(file.Value.Content, row.ContentType, row.OriginalFileName));

    }

    public async Task<ServiceResult<IReadOnlyList<LeaveAttachmentDto>>> ListAsync(Guid userId, Guid requestId, CancellationToken ct = default)
    {
        var access = await AuthorizeAysnc(userId, requestId, ct);
        if (access is null) return ServiceResult<IReadOnlyList<LeaveAttachmentDto>>.Fail(NotFound, ServiceErrorType.NotFound);

        var rows = await attachments.ListAsync(new LeaveAttachmentsByRequestSpecification(requestId), ct);
        var names = await users.GetDisplayNamesAsync(rows.Select(r => r.UploadedByUserId).Distinct().ToList(), tenant.RequiredTenantId, ct);

        return ServiceResult<IReadOnlyList<LeaveAttachmentDto>>.Success(rows.Select(r => new LeaveAttachmentDto(
            r.Id,
            r.OriginalFileName,
            r.ContentType,
            r.SizeBytes,
            names.GetValueOrDefault(r.UploadedByUserId, "Unknown"),
            r.CreatedAtUtc)).ToList());
    }

    public async Task<ServiceResult<LeaveAttachmentDto>> UploadAsync(Guid userId, Guid requestId, string fileName, Stream content, long length, CancellationToken ct = default)
    {
        var access = await AuthorizeAysnc(userId, requestId, ct);
        if (access is null) return ServiceResult<LeaveAttachmentDto>.Fail(NotFound, ServiceErrorType.NotFound);

        if (!access.IsOwner) return ServiceResult<LeaveAttachmentDto>.Fail(NotFound, ServiceErrorType.NotFound);
        if (access.Request.Status != ApprovalStatus.Pending)
            return ServiceResult<LeaveAttachmentDto>.Fail("Documents can only be attached while the request is pending. Ask HR.", ServiceErrorType.Conflict);

        var safeName = Path.GetFileName(fileName);
        var head = new byte[16];
        var read = await content.ReadAsync(head, ct);
        content.Position = 0;

        var existing = await attachments.CountAsync(new LeaveAttachmentsByRequestSpecification(requestId), ct);
        var check = LeaveAttachmentRules.Check(safeName, length, head.AsSpan(0, read), existing);
        if (check.Error is not null) return ServiceResult<LeaveAttachmentDto>.Fail(check.Error);

        var key = $"tenant/{tenant.RequiredTenantId}/leave-requests/{requestId}/{Guid.NewGuid():N}-{safeName}";
        var stored = await storage.SaveAsync(key, content, safeName, check.ContentType!, ct);

        var row = new LeaveRequestAttachment
        {
            LeaveRequestId = requestId,
            StorageKey = stored.StorageKey,
            OriginalFileName = safeName,
            ContentType = check.ContentType!,
            SizeBytes = length,
            UploadedByUserId = userId
        };

        attachments.Add(row);

        try { await unit.Complete(); }
        catch
        {
            await storage.DeleteAsync(stored.StorageKey, ct);
            throw;
        }

        return ServiceResult<LeaveAttachmentDto>.Success(new LeaveAttachmentDto(
            row.Id,
            row.OriginalFileName,
            row.ContentType,
            row.SizeBytes,
            "You",
            row.CreatedAtUtc));
    }
}