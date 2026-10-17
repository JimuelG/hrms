using Core.Entities;
using Core.Enums;

namespace Core.Specifications;
public sealed class LeaveRequestsByEmployeeSpecification : BaseSpecfication<LeaveRequest>
{
    public LeaveRequestsByEmployeeSpecification(Guid employeeId, ApprovalStatus? status, int? skip = null, int? take = null)
        : base(r => r.EmployeeId == employeeId && (status == null || r.Status == status))
    {
        AddOrderByDescending(r => r.StartDate);
        if (skip is not null && take is not null) ApplyPaging(skip.Value, take.Value);
    }
}

public sealed class ActiveLeaveRequestsByEmployeeSpecification : BaseSpecfication<LeaveRequest>
{
    private static readonly ApprovalStatus[] Active = [ApprovalStatus.Pending, ApprovalStatus.Approved];

    public ActiveLeaveRequestsByEmployeeSpecification(Guid employeeId, DateOnly from, DateOnly to)
        : base(r => r.EmployeeId == employeeId && Active.Contains(r.Status) && r.StartDate <= to && r.EndDate >= from) { }
}

public sealed class PendingLeaveRequestsSpecification : BaseSpecfication<LeaveRequest>
{
    public PendingLeaveRequestsSpecification(Guid employeeId, Guid leaveTypeId, int leaveYear)
        : base(r => r.EmployeeId == employeeId && r.LeaveTypeId == leaveTypeId
            && r.LeaveYear == leaveYear && r.Status == ApprovalStatus.Pending) { }
}

public sealed class LeaveRequestQuerySpecification : BaseSpecfication<LeaveRequest>
{
    public LeaveRequestQuerySpecification(ApprovalStatus? status, Guid? employeeId, DateOnly? from, DateOnly? to, int? skip = null, int? take = null)
        : base(r => (status == null || r.Status == status)
            && (employeeId == null || r.EmployeeId == employeeId)
            && (from == null || r.EndDate >= from)
            && (to == null || r.StartDate <= to))
    {
        AddOrderByDescending(r => r.StartDate);
        if (skip is not null && take is not null) ApplyPaging(skip.Value, take.Value);
    }
}

public sealed class LeaveRequestByTypeSpecification : BaseSpecfication<LeaveRequest>
{
    public LeaveRequestByTypeSpecification(Guid leaveTypeId) : base(r => r.LeaveTypeId == leaveTypeId) { }
}

public sealed class LeaveAttachmentsByRequestsSpecification : BaseSpecfication<LeaveRequestAttachment>
{
    public LeaveAttachmentsByRequestsSpecification(List<Guid> requestIds) : base(a => requestIds.Contains(a.LeaveRequestId)) { }
}

public sealed class LeaveAttachmentsByRequestSpecification : BaseSpecfication<LeaveRequestAttachment>
{
    public LeaveAttachmentsByRequestSpecification(Guid requestId) : base(a => a.LeaveRequestId == requestId)
    {
        AddOrderBy(a => a.CreatedAtUtc);
    }
}