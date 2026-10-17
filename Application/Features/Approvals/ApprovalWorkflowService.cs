using Core.Common;
using Core.DTOs.Approvals;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Approvals;

public class ApprovalWorkflowService(
    IUnitOfWork unit,
    IGenericRepository<ApprovalRequest> requests,
    IGenericRepository<ApprovalEvent> events,
    IGenericRepository<Employee> employees,
    IPermissionDirectory permissionDirectory,
    IPermissionService permissions,
    IApprovalQueryService query,
    IEnumerable<IApprovalSubjectHandler> handlers,
    ITenantContext tenant,
    TimeProvider clock) : IApprovalWorkFlowService
{
    private const string NotFoundMessage = "Approval request not found.";

    public Task<ServiceResult<ApprovalRequestDetailDto>> ApprovedAsync(Guid actorUserId, Guid requestId, string? note, CancellationToken ct = default) =>
        DecideAsync(actorUserId, requestId, approve: true, note, ct);

    public async Task<ServiceResult<ApprovalRequestDetailDto>> CancelAsync(Guid actorUserId, ApprovalSubjectType subjectType, Guid subjectId, string? note, CancellationToken ct = default)
    {
        var request = await requests.GetEntityWithSpec(new ApprovalRequestBySubjectSpecification(subjectType, subjectId), ct);
        if (request is null) return Fail(NotFoundMessage, ServiceErrorType.NotFound);

        var viewer = await ViewerAsync(actorUserId, ct);
        var states = ToStates(request);
        var access = ApprovalAccessRules.Evaluate(viewer, request.RequesterEmployeeId, request.Status, states);

        if (!access.CanSeeRequest) return Fail(NotFoundMessage, ServiceErrorType.NotFound);
        if (!access.IsRequester) return Fail("Only the person who made the request can cancel it.");
        if (request.Status != ApprovalStatus.Pending) return Fail("Only a pending request can be cancelled.", ServiceErrorType.Conflict);

        var noteCheck = ApprovalAccessRules.CheckNote(ApprovalEventType.Cancelled, note);
        if (noteCheck.Error is not null) return Fail(noteCheck.Error);

        var outcome = ApprovalStateMachine.Cancel(states);
        if (outcome.Error is not null) return Fail(outcome.Error, ServiceErrorType.Conflict);

        var handler = HandlerFor(request.SubjectType);
        if (handler is null) return Fail("No handler is registered for this kind of request.");

        var now = clock.GetUtcNow().UtcDateTime;
        await handler.OnCancelledAsync(new ApprovalSubjectContext(request.Id, request.SubjectType, request.SubjectId, request.RequesterEmployeeId, request.Title, actorUserId), ct);

        ApplyOutcome(request, outcome);
        request.Status = ApprovalStatus.Cancelled;
        request.LastActivityAtUtc = now;
        request.CompletedAtUtc = now;

        events.Add(new ApprovalEvent
        {
            ApprovalRequestId = request.Id,
            Type = ApprovalEventType.Cancelled,
            ActorUserId = actorUserId,
            Note = noteCheck.Note
        });

        await unit.Complete();

        return ServiceResult<ApprovalRequestDetailDto>.Success(await ReloadAndDescribeAsync(request.Id, actorUserId, ct));
    }

    public async Task<ServiceResult<ApprovalRequestDetailDto>> CommentAsync(Guid actorUserId, Guid requestId, string? note, CancellationToken ct = default)
    {
        var noteCheck = ApprovalAccessRules.CheckNote(ApprovalEventType.Commented, note);
        if (noteCheck.Error is not null) return Fail(noteCheck.Error);

        var request = await requests.GetEntityWithSpec(new ApprovalRequestWithStepsSpecification(requestId), ct);
        if (request is null) return Fail(NotFoundMessage, ServiceErrorType.NotFound);

        var viewer = await ViewerAsync(actorUserId, ct);
        var access = ApprovalAccessRules.Evaluate(viewer, request.RequesterEmployeeId, request.Status, ToStates(request));

        if (!access.CanSeeRequest) return Fail(NotFoundMessage, ServiceErrorType.NotFound);
        if (request.Status != ApprovalStatus.Pending) return Fail("Commnents are closed on a decided request.", ServiceErrorType.Conflict);

        events.Add(new ApprovalEvent
        {
            ApprovalRequestId = request.Id,
            Type = ApprovalEventType.Commented,
            ActorUserId = actorUserId,
            Note = noteCheck.Note
        });

        await unit.Complete();

        return ServiceResult<ApprovalRequestDetailDto>.Success(await ReloadAndDescribeAsync(request.Id, actorUserId, ct));
    }

    public Task<ServiceResult<ApprovalRequestDetailDto>> RejectAsync(Guid actorUserId, Guid requestId, string? note, CancellationToken ct = default) =>
        DecideAsync(actorUserId, requestId, approve: false, note, ct);

    public async Task<ServiceResult<SubmissionResult>> StageSubmissionAsync(ApprovalSubmission submission, CancellationToken ct = default)
    {
        var requester = await employees.GetByIdAsync(submission.RequesterEmployeeId, ct);
        if (requester is null)
            return ServiceResult<SubmissionResult>.Fail("Requester not found.", ServiceErrorType.NotFound);

        if (await requests.CountAsync(new ApprovalRequestExistsForSubjectSpecification(submission.SubjectType, submission.SubjectId), ct)  > 0)
            return ServiceResult<SubmissionResult>.Fail("This has already been submitted for approval.", ServiceErrorType.Conflict);

        var route = ApprovalRoutes.For(submission.SubjectType);

        var context = await RequesterContextAsync(requester, ct);
        var holders = new PermissionHolders(await permissionDirectory.GetHolderUserIdsAsync(route.Where(r => r.Permission is not null).Select(r => r.Permission!).ToList(), ct));

        var plan = ApprovalRoutePlanner.Plan(route, context, holders);
        if (plan.Error is not null) return ServiceResult<SubmissionResult>.Fail(plan.Error);

        var now = clock.GetUtcNow().UtcDateTime;
        var request = new ApprovalRequest
        {
            SubjectType = submission.SubjectType,
            SubjectId = submission.SubjectId,
            RequesterEmployeeId = requester.Id,
            Title = submission.Title,
            Details = submission.Details,
            Status = ApprovalStatus.Pending,
            LastActivityAtUtc = now};

        foreach (var s in plan.Steps)
        {
            request.Steps.Add(new ApprovalStep
            {
                Order = s.Order,
                Kind = s.Kind,
                Label = s.Label,
                Permission = s.Permission,
                ApproverEmployeeId = s.ApproverEmployeeId,
                Status = s.Status,
                SkipReason = s.SkipReason
            });
        }

        var tick = 0;
        DateTime Next() => now.AddMilliseconds(tick++);

        request.Events.Add(new ApprovalEvent { Type = ApprovalEventType.Submitted, ActorUserId = submission.ActorUserId, CreatedAtUtc = Next() });
        foreach (var skipped in plan.Steps.Where(s => s.Status == ApprovalStepStatus.Skipped))
        {
            request.Events.Add(new ApprovalEvent
            {
                Type = ApprovalEventType.StepSkipped,
                StepOrder = skipped.Order,
                Note = skipped.SkipReason,
                CreatedAtUtc = Next()
            });
        }

        requests.Add(request);

        return ServiceResult<SubmissionResult>.Success(new SubmissionResult(
            request.Id,
            plan.Steps.FirstOrDefault(s => s.Status == ApprovalStepStatus.Pending)?.Label,
            plan.Steps.Where(s => s.SkipReason is not null).Select(s => s.SkipReason!).ToList()));
    }

    private async Task<RequesterContext> RequesterContextAsync(Employee requester, CancellationToken ct)
    {
        var managerIsEmployed = false;
        var managerHasLogin = false;

        if (requester.ManagerId is Guid managerId)
        {
            var manager = await employees.GetByIdAsync(managerId, ct);
            managerIsEmployed = manager is not null && LeaveEligibility.IsEmployed(manager.Status);
            managerHasLogin = manager?.UserId is not null;
        }

        return new RequesterContext(requester.Id, requester.UserId, requester.ManagerId, managerIsEmployed, managerHasLogin);
    }

    private async Task<ServiceResult<ApprovalRequestDetailDto>> DecideAsync(Guid actorUserId, Guid requestId, bool approve, string? note, CancellationToken ct)
    {
        var eventType = approve ? ApprovalEventType.StepApproved : ApprovalEventType.Rejected;
        var noteCheck = ApprovalAccessRules.CheckNote(eventType, note);
        if (noteCheck.Error is not null) return Fail(noteCheck.Error);

        var request = await requests.GetEntityWithSpec(new ApprovalRequestWithStepsSpecification(requestId), ct);
        if (request is null) return Fail(NotFoundMessage, ServiceErrorType.NotFound);

        var viewer = await ViewerAsync(actorUserId, ct);
        var states = ToStates(request);
        var access = ApprovalAccessRules.Evaluate(viewer, request.RequesterEmployeeId, request.Status, states);

        if (!access.CanSeeRequest) return Fail(NotFoundMessage, ServiceErrorType.NotFound);

        if (request.Status != ApprovalStatus.Pending)
            return Fail("This request has already been decided.", ServiceErrorType.Conflict);

        if (access.IsRequester) return Fail("You can't decide your own request.");
        if (!access.CanDecideNow) return Fail("This request isn't waiting on you.");

        var current = request.Steps.Single(s => s.Status == ApprovalStepStatus.Pending);
        var outcome = approve
            ? ApprovalStateMachine.Approve(states, current.Order)
            : ApprovalStateMachine.Reject(states, current.Order);
        if (outcome.Error is not null) return Fail(outcome.Error, ServiceErrorType.Conflict);

        var now = clock.GetUtcNow().UtcDateTime;
        var subject = new ApprovalSubjectContext(request.Id, request.SubjectType, request.SubjectId, request.RequesterEmployeeId, request.Title, actorUserId);

        if (outcome.IsFinal)
        {
            var handler = HandlerFor(request.SubjectType);
            if (handler is null) return Fail("No handler is registered for this kind of request.");

            if (outcome.RequestStatus == ApprovalStatus.Approved)
            {
                var veto = await handler.OnApprovedAsync(subject, ct);
                if (veto is not null) return Fail($"This request can no longer be approved: {veto}");
            }
            else
            {
                await handler.OnRejectedAsync(subject, ct);
            }
        }

        ApplyOutcome(request, outcome);

        current.DecidedByUserId = actorUserId;
        current.DecidedAtUtc = now;
        current.Note = noteCheck.Note;

        request.Status = outcome.RequestStatus;
        request.LastActivityAtUtc = now;
        if (outcome.IsFinal) request.CompletedAtUtc = now;

        events.Add(new ApprovalEvent
        {
            ApprovalRequestId = request.Id,
            Type = eventType,
            StepOrder = current.Order,
            ActorUserId = actorUserId,
            Note = noteCheck.Note
        });

        await unit.Complete();

        return ServiceResult<ApprovalRequestDetailDto>.Success(await ReloadAndDescribeAsync(request.Id, actorUserId, ct));   
    }

    private static void ApplyOutcome(ApprovalRequest request, StepOutcome outcome)
    {
        foreach (var next in outcome.Steps)
        {
            var step = request.Steps.Single(s => s.Order == next.Order);
            step.Status = next.Status;
        }
    }

    private IApprovalSubjectHandler? HandlerFor(ApprovalSubjectType type) =>
        handlers.FirstOrDefault(h => h.SubjectType == type);

    private async Task<ApprovalAccessRules.Viewer> ViewerAsync(Guid userId, CancellationToken ct)
    {
        var employee = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        var codes = await permissions.GetPermissionsAsync(userId, tenant.RequiredTenantId, ct);
        return new ApprovalAccessRules.Viewer(userId, employee?.Id, codes.ToHashSet()); 
    }

    private static IReadOnlyList<StepState> ToStates(ApprovalRequest request) =>
        request.Steps.OrderBy(s => s.Order)
            .Select(s => new StepState(s.Order, s.Kind, s.Label, s.Permission, s.ApproverEmployeeId, s.Status, s.SkipReason))
            .ToList();

    private async Task<ApprovalRequestDetailDto> ReloadAndDescribeAsync(Guid requestId, Guid viewerUserId, CancellationToken ct)
    {
        var fresh = await requests.GetEntityWithSpec(new ApprovalRequestWithStepsSpecification(requestId), ct);
        return await query.DescribeAsync(fresh!, viewerUserId, ct);
    }

    private static ServiceResult<ApprovalRequestDetailDto> Fail(string message, ServiceErrorType type = ServiceErrorType.Validation) =>
        ServiceResult<ApprovalRequestDetailDto>.Fail(message, type);
}