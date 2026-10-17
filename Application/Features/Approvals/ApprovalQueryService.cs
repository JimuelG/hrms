using Core.Common;
using Core.DTOs.Approvals;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Approvals;

public sealed class ApprovalQueryService(
    IGenericRepository<ApprovalRequest> requests,
    IGenericRepository<ApprovalStep> steps,
    IGenericRepository<ApprovalEvent> events,
    IGenericRepository<Employee> employees,
    IUserDirectory users,
    IPermissionService permissions,
    ITenantContext tenant) : IApprovalQueryService
{
    private const string NotFoundMessage = "Approval request not found.";
    private const string NotLinked = "No employee profile is linked to your account. Ask HR to link it";
    private const int MaxPageSize = 50;

    private sealed record PersonName(string Name, string Number);

    public async Task<ApprovalRequestDetailDto> DescribeAsync(ApprovalRequest request, Guid viewerUserId, CancellationToken ct = default)
    {
        var viewer = await ViewerAsync(viewerUserId, ct);
        var access = ApprovalAccessRules.Evaluate(viewer, request.RequesterEmployeeId, request.Status, ToStates(request));
        return await BuildDetailAsync(request, access, ct);
    }

    public async Task<ServiceResult<ApprovalRequestDetailDto>> GetAsync(Guid userId, Guid requestId, CancellationToken ct = default)
    {
        var request = await requests.GetEntityWithSpec(new ApprovalRequestWithStepsSpecification(requestId), ct);
        return await AuthorizeAndDescribeAsync(request, userId, ct);
    }

    public async Task<ServiceResult<ApprovalRequestDetailDto>> GetBySubjectAsync(Guid userId, ApprovalSubjectType subjectType, Guid subjectId, CancellationToken ct = default)
    {
        var request = await requests.GetEntityWithSpec(new ApprovalRequestBySubjectSpecification(subjectType, subjectId), ct);
        return await AuthorizeAndDescribeAsync(request, userId, ct);
    }

    public async Task<ServiceResult<PagedResult<ApprovalRequestSummaryDto>>> GetInboxAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Clamp(page, pageSize);

        var viewer = await ViewerAsync(userId, ct);
        var employeeId = viewer.EmployeeId ?? Guid.Empty;
        var permissionCodes = viewer.Permissions.ToArray();

        var total = await steps.CountAsync(new ApprovalInboxSpecification(employeeId, permissionCodes), ct);
        var rows = await steps.ListAsync(new ApprovalInboxSpecification(employeeId, permissionCodes, (page - 1) * pageSize), ct);

        var people = await EmployeeNameAsync(rows.Select(r => r.Request.RequesterEmployeeId).Distinct().ToList(), ct);

        var items = rows.Select(r => ToSummary(r.Request, people, r.Label)).ToList();

        return ServiceResult<PagedResult<ApprovalRequestSummaryDto>>.Success(new PagedResult<ApprovalRequestSummaryDto>(items, total, page, pageSize));
    }

    public async Task<ServiceResult<PagedResult<ApprovalRequestSummaryDto>>> GetMineAsync(Guid userId, ApprovalStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Clamp(page, pageSize);

        var employee = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        if (employee is null) return ServiceResult<PagedResult<ApprovalRequestSummaryDto>>.Fail(NotLinked, ServiceErrorType.NotFound);

        var total = await requests.CountAsync(new ApprovalRequestsByRequesterSpecification(employee.Id, status), ct);
        var rows = await requests.ListAsync(new ApprovalRequestsByRequesterSpecification(employee.Id, status, (page -1) * pageSize, pageSize), ct);

        var people = await EmployeeNameAsync([employee.Id], ct);
        var items = rows.Select(r => ToSummary(r, people, null)).ToList();

        return ServiceResult<PagedResult<ApprovalRequestSummaryDto>>.Success(new PagedResult<ApprovalRequestSummaryDto>(items, total, page, pageSize));
    }

    private async Task<ServiceResult<ApprovalRequestDetailDto>> AuthorizeAndDescribeAsync(ApprovalRequest? request, Guid userId, CancellationToken ct)
    {
        if (request is null)
            return ServiceResult<ApprovalRequestDetailDto>.Fail(NotFoundMessage, ServiceErrorType.NotFound);

        var viewer = await ViewerAsync(userId, ct);
        var access = ApprovalAccessRules.Evaluate(viewer, request.RequesterEmployeeId, request.Status, ToStates(request));

        if (!access.CanSeeRequest)
            return ServiceResult<ApprovalRequestDetailDto>.Fail(NotFoundMessage, ServiceErrorType.NotFound);

        return ServiceResult<ApprovalRequestDetailDto>.Success(await BuildDetailAsync(request, access, ct));
    }

    private async Task<ApprovalRequestDetailDto> BuildDetailAsync(
        ApprovalRequest request, ApprovalAccess access, CancellationToken ct)
    {
        var orderedSteps = request.Steps.OrderBy(s => s.Order).ToList();
        var history = await events.ListAsync(new ApprovalEventsByRequestSpecification(request.Id), ct);

        var employeeIds = orderedSteps.Where(s => s.ApproverEmployeeId is not null)
            .Select(s => s.ApproverEmployeeId!.Value)
            .Append(request.RequesterEmployeeId).Distinct().ToList();
        var people = await EmployeeNameAsync(employeeIds, ct);

        var userIds = orderedSteps.Where(s => s.DecidedByUserId is not null).Select(s => s.DecidedByUserId!.Value)
            .Concat(history.Where(e => e.ActorUserId is not null).Select(e => e.ActorUserId!.Value))
            .Distinct().ToList();
        var userNames = await users.GetDisplayNamesAsync(userIds, tenant.RequiredTenantId, ct);

        var stepDtos = orderedSteps.Select(s => new ApprovalStepDto(
            s.Order,
            (int)s.Kind,
            s.Label,
            (int)s.Status,
            s.ApproverEmployeeId is Guid a ? NameOf(people, a) : null,
            s.SkipReason,
            s.DecidedByUserId is Guid d && userNames.TryGetValue(d, out var decidedBy) ? decidedBy : null,
            s.DecidedAtUtc,
            s.Note)).ToList();

        var eventDtos = history.Select(e => new ApprovalEventDto(
            (int)e.Type,
            e.StepOrder,
            e.ActorUserId is Guid u && userNames.TryGetValue(u, out var actor) ? actor : null,
            e.Note,
            e.CreatedAtUtc)).ToList();

        var pending = request.Status == ApprovalStatus.Pending;
        var accessDto = new ApprovalAccessDto(
            access.IsRequester,
            access.CanDecideNow,
            CanComment: pending && access.CanSeeRequest,
            CanCancel: pending && access.IsRequester);

        return new ApprovalRequestDetailDto(ToSummary(request, people, null), stepDtos, eventDtos, accessDto);
    }

    private async Task<ApprovalAccessRules.Viewer> ViewerAsync(Guid userId, CancellationToken ct)
    {
        var employee = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        var codes = await permissions.GetPermissionsAsync(userId, tenant.RequiredTenantId, ct);
        return new ApprovalAccessRules.Viewer(userId, employee?.Id, codes.ToHashSet());
    }

    private async Task<Dictionary<Guid, PersonName>> EmployeeNameAsync(List<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return [];

        var rows = await employees.ListAsync(new EmployeesByIdsSpecification(ids), ct);
        return rows.ToDictionary(e => e.Id, e => new PersonName($"{e.FirstName} {e.LastName}", e.EmployeeNumber));
    }

    private static string NameOf(Dictionary<Guid, PersonName> people, Guid id) =>
        people.TryGetValue(id, out var p) ? p.Name : "Unknown employee";

    private static IReadOnlyList<StepState> ToStates(ApprovalRequest request) =>
        request.Steps.OrderBy(s => s.Order)
            .Select(s => new StepState(s.Order, s.Kind, s.Label, s.Permission, s.ApproverEmployeeId, s.Status, s.SkipReason)).ToList();

    private static ApprovalRequestSummaryDto ToSummary(
        ApprovalRequest r, Dictionary<Guid, PersonName> people, string? currentStepLabel)
    {
        var label = currentStepLabel
            ?? (r.Status == ApprovalStatus.Pending
                ? r.Steps.FirstOrDefault(s => s.Status == ApprovalStepStatus.Pending)?.Label
                : null);

        people.TryGetValue(r.RequesterEmployeeId, out var requester);

        return new ApprovalRequestSummaryDto(
            r.Id,
            (int)r.SubjectType,
            r.SubjectId,
            r.RequesterEmployeeId,
            requester?.Name ?? "Unknown employee",
            requester?.Number ?? "",
            r.Title,
            r.Details,
            (int)r.Status,
            label,
            r.CreatedAtUtc,
            r.CompletedAtUtc);
    }

    private static (int Page, int PageSize) Clamp(int page, int pagesize) =>
        (Math.Max(page, 1), Math.Clamp(pagesize, 1, MaxPageSize));
}
