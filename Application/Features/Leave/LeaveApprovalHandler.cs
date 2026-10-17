using Core.Common;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Leave;

public class LeaveApprovalHandler(
    IGenericRepository<LeaveRequest> requests,
    IGenericRepository<LeaveRequestAttachment> attachments,
    IGenericRepository<LeaveBalanceEntry> ledger,
    IGenericRepository<LeaveType> leaveTypes,
    IGenericRepository<Employee> employees,
    ILeaveCalculationService calculation,
    ITimeZoneResolver zones,
    TimeProvider clock) : IApprovalSubjectHandler
{
    public ApprovalSubjectType SubjectType => ApprovalSubjectType.LeaveRequest;

    public async Task<string?> OnApprovedAsync(ApprovalSubjectContext context, CancellationToken ct = default)
    {
        var request = await requests.GetByIdAsync(context.SubjectId, ct);
        if (request is null) return "the leave request no longer exists.";

        var employee = await employees.GetByIdAsync(request.EmployeeId, ct);
        if (employee is null) return "the employee no longer exists.";

        var type = await leaveTypes.GetByIdAsync(request.LeaveTypeId, ct);
        if (type is null) return "the leave type no longer exists.";

        var now = clock.GetUtcNow().UtcDateTime;
        var currentYear = LocalClock.LocalDate(now, await zones.ResolveAsync(employee, ct)).Year;

        decimal balance = 0;
        if (type.EntitlementMode != LeaveEntitlementMode.Unlimited)
        {
            var rows = (await ledger.ListAsync(new LeaveEntriesByEmployeeSpecification(employee.Id), ct))
                .Where(r => r.LeaveTypeId == type.Id && r.Year == request.LeaveYear)
                .Select(r => new LedgerRow(r.Year, r.EntryType, r.Days, r.IsAutomatic));
            balance = LeaveBalanceMath.Summarize(rows).Balance;
        }

        var documentCount = await attachments.CountAsync(new LeaveAttachmentsByRequestSpecification(request.Id), ct);

        var veto = LeaveApprovalRules.CheckFinalApproval(
            request.Status,
            employee.Status,
            type.EntitlementMode,
            request.LeaveYear,
            currentYear,
            balance,
            request.Days,
            LeaveCalculator.RequiresDocument(request.Days, type.RequiresDocumentAfterDays),
            documentCount);
        if (veto is not null) return veto;

        veto = await FindApprovedOverlapsAsync(employee, type, request, ct);
        if (veto is not null) return veto;


        request.Status = ApprovalStatus.Approved;
        request.DecidedAtUtc = now;

        if (type.EntitlementMode != LeaveEntitlementMode.Unlimited)
        {
            ledger.Add(new LeaveBalanceEntry
            {
                EmployeeId = employee.Id,
                LeaveTypeId = type.Id,
                Year = request.LeaveYear,
                EntryType = LeaveEntryType.Usage,
                Days = -request.Days,
                Description = $"{type.Name}: {request.StartDate:MMM d}" + (request.StartDate == request.EndDate ? "" : $" to {request.EndDate:MMM d}"),
                IsAutomatic = false,
                CreatedByUserId = context.ActorUserId,
                LeaveRequestId = request.Id
            });
        }

        return null;
    }

    public async Task OnCancelledAsync(ApprovalSubjectContext context, CancellationToken ct = default)
    {
        var request = await requests.GetByIdAsync(context.SubjectId, ct);
        if (request is null) return;


        request.Status = ApprovalStatus.Cancelled;
        request.CancelledAtUtc = clock.GetUtcNow().UtcDateTime;
        request.CancelledByUserId = context.ActorUserId;
    }

    public async Task OnRejectedAsync(ApprovalSubjectContext context, CancellationToken ct = default)
    {
        var request = await requests.GetByIdAsync(context.SubjectId, ct);
        if (request is null) return;

        request.Status = ApprovalStatus.Rejected;
        request.DecidedAtUtc = clock.GetUtcNow().UtcDateTime;
    }

    private async Task<string?> FindApprovedOverlapsAsync(Employee employee, LeaveType type, LeaveRequest request, CancellationToken ct)
    {
        var others = (await requests.ListAsync(
            new ActiveLeaveRequestsByEmployeeSpecification(employee.Id, request.StartDate, request.EndDate), ct))
            .Where(r => r.Id != request.Id && r.Status == ApprovalStatus.Approved)
            .ToList();
        if (others.Count == 0) return null;

        var mine = await CountedSlotsAsync(employee, type, request, ct);
        if (mine is null) return "its dates can no longer be calculated (the schedule, holidays or leave type changed).";

        foreach (var other in others)
        {
            var otherType = await leaveTypes.GetByIdAsync(other.LeaveTypeId, ct);
            if (otherType is null) continue;

            var theirs = await CountedSlotsAsync(employee, otherType, other, ct);
            if (theirs is not null && LeaveOverlap.Overlaps(mine, theirs))
                return $"it overlaps approved leave from {other.StartDate:MMM d} to {other.EndDate:MMM d}.";
        }

        return null;
    }

    private async Task<HashSet<(DateOnly Date, int Half)>?> CountedSlotsAsync(Employee employee, LeaveType type, LeaveRequest request, CancellationToken ct)
    {
        var calc = await calculation.CalculateAsync(employee, type, request.StartDate, request.EndDate, request.StartPortion, request.EndPortion, ct);
        if (!calc.Succeeded) return null;

        return LeaveOverlap.Slots(
            request.StartDate, 
            request.EndDate, 
            request.StartPortion, 
            request.EndPortion,
            calc.Value!.Lines.Where(l => l.Status == LeaveDayStatus.Counted).Select(l => l.Date));
    }
}