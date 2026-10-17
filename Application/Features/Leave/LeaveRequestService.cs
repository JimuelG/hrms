using Core.Common;
using Core.Constant;
using Core.DTOs.Approvals;
using Core.DTOs.Leave;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Leave;

public class LeaveRequestService(
    IUnitOfWork unit,
    IGenericRepository<LeaveRequest> requests,
    IGenericRepository<LeaveRequestAttachment> attachments,
    IGenericRepository<LeaveBalanceEntry> ledger,
    IGenericRepository<LeaveType> leaveTypes,
    IGenericRepository<Employee> employees,
    IGenericRepository<ApprovalRequest> approvals,
    IGenericRepository<ApprovalEvent> approvalEvents,
    ILeaveCalculationService calculation,
    ILeaveBalanceService balances,
    IApprovalWorkFlowService workflow,
    IPermissionService permissions,
    ITenantContext tenant,
    ITimeZoneResolver zones,
    TimeProvider clock) : ILeaveRequestService
{
    private const string NotLinked = "No employee profile is linked to your account. Ask HR to link it.";
    private const string NotFound = "Leave request not found.";
    private const int MaxPageSize = 50;

    public async Task<ServiceResult<LeaveFilingRequestDto>> FileAsync(Guid userId, CreateLeaveRequestDto dto, CancellationToken ct = default)
    {
        var employee = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        if (employee is null) return Fail(NotLinked, ServiceErrorType.NotFound);

        var type = await leaveTypes.GetByIdAsync(dto.LeaveTypeId!.Value, ct);
        if (type is null) return Fail("Leave type not found.", ServiceErrorType.NotFound);
        if (!type.IsActive) return Fail("That leave type is no longer available.");

        if (!Enum.IsDefined(typeof(DayPortion), dto.StartPortion) || !Enum.IsDefined(typeof(DayPortion), dto.EndPortion))
            return Fail("Invalid day portion.");

        var reason = dto.Reason.Trim();
        if (reason.Length == 0) return Fail("Give a reason for the request.");

        var start = dto.StartDate!.Value;
        var end = dto.EndDate!.Value;
        var today = LocalClock.LocalDate(clock.GetUtcNow().UtcDateTime, await zones.ResolveAsync(employee, ct));

        var eligibility = LeaveFilingRules.CheckEligibility(employee.Status, employee.HireDate, type.MinServiceMonths, type.Name, start);
        if (eligibility is not null) return Fail(eligibility);

        var dateError = LeaveFilingRules.CheckDates(start, end, today);
        if (dateError is not null) return Fail(dateError);

        var calc = await calculation.CalculateAsync(
            employee,
            type,
            start,
            end,
            (DayPortion)dto.StartPortion,
            (DayPortion)dto.EndPortion,
            ct);

        if (!calc.Succeeded) return Fail(calc.Error!, calc.ErrorType);
        var computed = calc.Value!;

        var futureYear = LeaveFilingRules.CheckFutureYear(type.EntitlementMode, start.Year, today.Year);
        if (futureYear is not null) return Fail(futureYear);

        var overlap = await FindOverlapAsync(employee, computed, start, end, dto, ct);
        if (overlap is not null) return Fail(overlap, ServiceErrorType.Conflict);

        var year = start.Year;
        if (type.EntitlementMode !=  LeaveEntitlementMode.Unlimited)
        {
            await balances.EnsureGrantsAsync(employee, ct);

            var balance = await YearBalanceAsync(employee.Id, type.Id, year, ct);
            var pending = (await requests.ListAsync(new PendingLeaveRequestsSpecification(employee.Id, type.Id, year), ct)).Sum(r => r.Days);

            var balanceError = LeaveFilingRules.CheckBalance(type.EntitlementMode, balance, pending, computed.Days);
            if (balanceError is not null) return Fail(balanceError);
        }

        var request = new LeaveRequest
        {
            EmployeeId = employee.Id,
            LeaveTypeId = type.Id,
            StartDate = start,
            EndDate = end,
            StartPortion = (DayPortion)dto.StartPortion,
            EndPortion = (DayPortion)dto.EndPortion,
            Days = computed.Days,
            LeaveYear = year,
            IsPaid = type.IsPaid,
            Reason = reason,
            Status = ApprovalStatus.Pending};

        requests.Add(request);

        var title =$"{type.Name}: {FormatDays(computed.Days)} ({start:MMM d}" + (start == end ? ")" : $" to {end:MMM d})"); 
        var submission = await workflow.StageSubmissionAsync(new ApprovalSubmission(
            ApprovalSubjectType.LeaveRequest,
            request.Id,
            employee.Id,
            title,
            reason,
            userId), ct);

        if (!submission.Succeeded)
        {
            return Fail(submission.Error!, submission.ErrorType);
        }

        await unit.Complete();

        var notes = new List<string>(submission.Value!.SkipNotes);
        var document = LeaveCalculator.RequiresDocument(computed.Days, type.RequiresDocumentAfterDays);
        if (document) notes.Add("A supporting document is required for a request this long. Attach it before approval.");

        var dtoOut = (await BuildAsync([request], ct)).Single();
        return ServiceResult<LeaveFilingRequestDto>.Success(new LeaveFilingRequestDto(dtoOut, notes));
    }

    public async Task<ServiceResult<LeaveRequestDto>> GetAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var request = await requests.GetByIdAsync(id, ct);
        if (request is null) return ServiceResult<LeaveRequestDto>.Fail(NotFound, ServiceErrorType.NotFound);

        var employee = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        var isOwner = employee?.Id == request.EmployeeId;

        if (!isOwner)
        {
            var codes = await permissions.GetPermissionsAsync(userId, tenant.RequiredTenantId, ct);
            var isHr = codes.Contains(Permissions.Leave.Manage);
            if (!isHr && !await IsApproverAsync(userId, employee?.Id, codes, request, ct));
                return ServiceResult<LeaveRequestDto>.Fail(NotFound, ServiceErrorType.NotFound);
        }

        return ServiceResult<LeaveRequestDto>.Success((await BuildAsync([request], ct)).Single());
    }

    public async Task<ServiceResult<PagedResult<LeaveRequestDto>>> GetMineAsync(Guid userId, ApprovalStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Clamp(page, pageSize);

        var employee = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        if (employee is null)
            return ServiceResult<PagedResult<LeaveRequestDto>>.Fail(NotLinked, ServiceErrorType.NotFound);

        var total = await requests.CountAsync(new LeaveRequestsByEmployeeSpecification(employee.Id, status), ct);
        var rows = await requests.ListAsync(new LeaveRequestsByEmployeeSpecification(employee.Id, status, (page - 1) * pageSize, pageSize), ct);

        return ServiceResult<PagedResult<LeaveRequestDto>>.Success(new PagedResult<LeaveRequestDto>(await BuildAsync(rows, ct), total, page, pageSize));
    }

    public async Task<ServiceResult<PagedResult<LeaveRequestDto>>> QueryAsync(ApprovalStatus? status, Guid? employeeId, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Clamp(page, pageSize); 

        if (from is not null && to is not null && to < from)
            return ServiceResult<PagedResult<LeaveRequestDto>>.Fail("'from' must be on or before 'to'.");

        var total = await requests.CountAsync(new LeaveRequestQuerySpecification(status, employeeId, from, to), ct);
        var rows = await requests.ListAsync(new LeaveRequestQuerySpecification(status, employeeId, from, to, (page - 1) * pageSize, pageSize), ct);

        return ServiceResult<PagedResult<LeaveRequestDto>>.Success(new PagedResult<LeaveRequestDto>(await BuildAsync(rows, ct), total, page, pageSize));
    }

    public async Task<ServiceResult<LeaveRequestDto>> CancelAsync(Guid userId, Guid id, string? reason, CancellationToken ct = default)
    {
        var request = await requests.GetByIdAsync(id, ct);
        if (request is null) return ServiceResult<LeaveRequestDto>.Fail(NotFound, ServiceErrorType.NotFound);

        var viewer = await employees.GetEntityWithSpec(new EmployeeByUserIdSpecification(userId), ct);
        var codes = (await permissions.GetPermissionsAsync(userId, tenant.RequiredTenantId, ct)).ToHashSet();
        var isRequester = viewer?.Id == request.EmployeeId;
        var isHr = codes.Contains(Permissions.Leave.Manage);

        if (!isRequester && !isHr && !await IsApproverAsync(userId, viewer?.Id, codes, request, ct))
            return ServiceResult<LeaveRequestDto>.Fail(NotFound, ServiceErrorType.NotFound);

        var owner = await employees.GetByIdAsync(request.EmployeeId, ct);
        var today = LocalClock.LocalDate(clock.GetUtcNow().UtcDateTime, owner is null ? null : await zones.ResolveAsync(owner, ct));

        var decision = LeaveCancellationRules.Decide(request.Status, request.StartDate, today, isRequester, isHr);
        if (decision.Kind is null) return ServiceResult<LeaveRequestDto>.Fail(decision.Error!);

        var text = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (text is { Length: > 500}) return ServiceResult<LeaveRequestDto>.Fail("The reason can be at most 500 characters.");

        if (decision.Kind == LeaveCancelKind.Withdraw)
        {
            request.CancellationReason = text;
            var withdrawn = await workflow.CancelAsync(userId, ApprovalSubjectType.LeaveRequest, request.Id, text, ct);
            if (!withdrawn.Succeeded)
            {
                request.CancellationReason = null;
                return ServiceResult<LeaveRequestDto>.Fail(withdrawn.Error!, withdrawn.ErrorType);
            }

            return ServiceResult<LeaveRequestDto>.Success((await BuildAsync([request], ct)).Single());
        }

        if (text is null) return ServiceResult<LeaveRequestDto>.Fail("Give a reason for cancelling approved leave.");

        var entries = await ledger.ListAsync(new LeaveEntriesByRequestSpecification(request.Id), ct);
        if (entries.Any(e => e.EntryType == LeaveEntryType.Reversal))
            return ServiceResult<LeaveRequestDto>.Fail("These days have already been returned.", ServiceErrorType.Conflict);

        var now = clock.GetUtcNow().UtcDateTime;
        request.Status = ApprovalStatus.Cancelled;
        request.CancelledAtUtc = now;
        request.CancelledByUserId = userId;
        request.CancellationReason = text;

        var usage = entries.FirstOrDefault(e => e.EntryType == LeaveEntryType.Usage);
        if (usage is not null)
        {
            var type = await leaveTypes.GetByIdAsync(request.LeaveTypeId, ct);
            ledger.Add(new LeaveBalanceEntry
            {
               EmployeeId = request.EmployeeId,
               LeaveTypeId = request.LeaveTypeId,
               Year = usage.Year,
               EntryType = LeaveEntryType.Reversal,
               Days = -usage.Days,
               Description = $"Cancelled: {type?.Name ?? "Leave"} {request.StartDate:MMM d}" + (request.StartDate == request.EndDate ? "" : $" to {request.EndDate:MMM d}"),
               IsAutomatic = false,
               CreatedByUserId = userId,
               LeaveRequestId = request.Id 
            });
        }
        var approval = (await approvals.ListAsync(new ApprovalRequestBySubjectSpecification(ApprovalSubjectType.LeaveRequest, request.Id), ct)).FirstOrDefault();
        if (approval is not null)
        {
            approvalEvents.Add(new ApprovalEvent
            {
               ApprovalRequestId = approval.Id,
               Type = ApprovalEventType.Cancelled,
               ActorUserId = userId,
               Note = text
            });
        }

        await unit.Complete();

        return ServiceResult<LeaveRequestDto>.Success((await BuildAsync([request], ct)).Single());
    }

    private async Task<string?> FindOverlapAsync(Employee employee, LeaveCalculation computed, DateOnly start, DateOnly end, CreateLeaveRequestDto dto, CancellationToken ct)
    {
        var existing = await requests.ListAsync(new ActiveLeaveRequestsByEmployeeSpecification(employee.Id, start, end), ct);
        if (existing.Count == 0) return null;

        var mine = LeaveOverlap.Slots(
            start,
            end,
            (DayPortion)dto.StartPortion,
            (DayPortion)dto.EndPortion,
            computed.Lines.Where(l => l.Status == LeaveDayStatus.Counted).Select(l => l.Date));

        foreach (var other in existing)
        {
            var type = await leaveTypes.GetByIdAsync(other.LeaveTypeId, ct);
            if (type is null) continue;

            var recalculated = await calculation.CalculateAsync(
                employee,
                type,
                other.StartDate,
                other.EndDate,
                other.StartPortion,
                other.EndPortion,
                ct);
            if (!recalculated.Succeeded) continue;

            var theirs = LeaveOverlap.Slots(
                other.StartDate,
                other.EndDate,
                other.StartPortion,
                other.EndPortion,
                recalculated.Value!.Lines.Where(l => l.Status == LeaveDayStatus.Counted).Select(l => l.Date));

            if (LeaveOverlap.Overlaps(mine, theirs))
                return $"This overlaps your {(other.Status == ApprovalStatus.Approved ? "approved" : "pending")} " +
                        $"leave from {other.StartDate:MMM d} to {other.EndDate:MMM d}.";
        }

        return null;
    }

    private async Task<decimal> YearBalanceAsync(Guid employeeId, Guid leaveTypeId, int year, CancellationToken ct)
    {
        var rows = (await ledger.ListAsync(new LeaveEntriesByEmployeeSpecification(employeeId), ct))
            .Where(r => r.LeaveTypeId == leaveTypeId && r.Year == year)
            .Select(r => new LedgerRow(r.Year, r.EntryType, r.Days, r.IsAutomatic));

        return LeaveBalanceMath.Summarize(rows).Balance;
    }

    private async Task<bool> IsApproverAsync(Guid userId, Guid? employeeId, IEnumerable<string> permissionsCodes, LeaveRequest request, CancellationToken ct)
    {
        var approval = (await approvals.ListAsync(new ApprovalRequestBySubjectSpecification(ApprovalSubjectType.LeaveRequest, request.Id), ct)).FirstOrDefault();
        if (approval is null) return false;

        var viewer = new ApprovalAccessRules.Viewer(userId, employeeId, permissionsCodes.ToHashSet());
        var states = approval.Steps.Select(s => new StepState(
            s.Order,
            s.Kind,
            s.Label,
            s.Permission,
            s.ApproverEmployeeId,
            s.Status,
            s.SkipReason)).ToList();
        return ApprovalAccessRules.Evaluate(viewer, approval.RequesterEmployeeId, approval.Status, states).CanSeeRequest;
    }

    private async Task<List<LeaveRequestDto>> BuildAsync(IReadOnlyList<LeaveRequest> rows, CancellationToken ct)
    {
        if (rows.Count == 0) return [];

        var employeeIds = rows.Select(r => r.EmployeeId).Distinct().ToList();
        var people = (await employees.ListAsync(new EmployeesByIdsSpecification(employeeIds), ct)).ToDictionary(e => e.Id);

        var types = (await leaveTypes.ListAsync(new LeaveTypeSearchSpecification(null, activeOnly: false), ct)).ToDictionary(t => t.Id);

        var requestIds = rows.Select(r => r.Id).ToList();
        var counts = (await attachments.ListAsync(new LeaveAttachmentsByRequestsSpecification(requestIds), ct))
            .GroupBy(a => a.LeaveRequestId).ToDictionary(g => g.Key, g => g.Count());
        
        var approvalByRequest = new Dictionary<Guid, ApprovalRequest>();

        foreach (var id in requestIds)
        {
            var approval = (await approvals.ListAsync(new ApprovalRequestBySubjectSpecification(ApprovalSubjectType.LeaveRequest, id), ct))
                .FirstOrDefault();
            if (approval is not null) approvalByRequest[id] = approval;
        }

        return rows.Select(r =>
        {
            people.TryGetValue(r.EmployeeId, out var person);
            types.TryGetValue(r.LeaveTypeId, out var type);
            approvalByRequest.TryGetValue(r.Id, out var approval);

            var documentRequired = type is not null && LeaveCalculator.RequiresDocument(r.Days, type.RequiresDocumentAfterDays);
            var stepLabel = approval is { Status: ApprovalStatus.Pending }
                ? approval.Steps.FirstOrDefault(s => s.Status == ApprovalStepStatus.Pending)?.Label
                : null;

            return new LeaveRequestDto(
                r.Id,
                r.EmployeeId,
                person is null ? "Unknown employee" : $"{person.FirstName} {person.LastName}",
                person?.EmployeeNumber ?? "",
                r.LeaveTypeId,
                type?.Code ?? "?",
                type?.Name ?? "Unknown",
                r.IsPaid,
                r.StartDate,
                r.EndDate,
                (int)r.StartPortion,
                (int)r.EndPortion,
                r.Days,
                r.LeaveYear,
                r.Reason,
                (int)r.Status,
                r.CreatedAtUtc,
                r.DecidedAtUtc, 
                r.CancelledAtUtc, 
                r.CancellationReason,
                documentRequired,
                counts.GetValueOrDefault(r.Id),
                approval?.Id,
                stepLabel);

        }).ToList();
    }

    private static string FormatDays(decimal days) => $"{days:0.#} day{(days == 1 ? "" : "s")}";

    private static (int Page, int PageSize) Clamp(int page, int pageSize) => (Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize));
    private static ServiceResult<LeaveFilingRequestDto> Fail(string message, ServiceErrorType type = ServiceErrorType.Validation) => 
        ServiceResult<LeaveFilingRequestDto>.Fail(message, type);
}