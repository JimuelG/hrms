using Core.Entities;
using Core.Enums;

namespace Core.Specifications;
public sealed class CorrectionQuerySpecification : BaseSpecfication<AttendanceCorrectionRequest>
{
    public CorrectionQuerySpecification(ApprovalStatus? status, Guid? employeeId, int? skip = null, int? take = null)
        : base(r => (status == null || r.Status == status) && (employeeId == null || r.EmployeeId == employeeId))
    {
        AddInclude(r => r.Employee);
        AddOrderByDescending(r => r.CreatedAtUtc);
        if (skip is not null && take is not null) ApplyPaging(skip.Value, take.Value);
    }
}

public sealed class CorrectionByIdWithEmployeeSpecification : BaseSpecfication<AttendanceCorrectionRequest>
{
    public CorrectionByIdWithEmployeeSpecification(Guid id) : base(r => r.Id == id)
    {
        AddInclude(r => r.Employee);
    }
}

public sealed class PendingCorrectionByEmployeeDateSpecification : BaseSpecfication<AttendanceCorrectionRequest>
{
    public PendingCorrectionByEmployeeDateSpecification(Guid employeeId, DateOnly date)
        : base(r => r.EmployeeId == employeeId && r.Date == date && r.Status == ApprovalStatus.Pending) { }
}

public sealed class OvertimeQuerySpecification : BaseSpecfication<OvertimeRequest>
{
    public OvertimeQuerySpecification(ApprovalStatus? status, Guid? employeeId, int? skip = null, int? take = null)
        : base(r => (status == null || r.Status == status) && (employeeId == null || r.EmployeeId == employeeId))
    {
        AddInclude(r => r.Employee);
        AddOrderByDescending(r => r.CreatedAtUtc);
        if (skip is not null && take is not null) ApplyPaging(skip.Value, take.Value);
    }
}

public sealed class OvertimeByIdWithEmployeeSpecification : BaseSpecfication<OvertimeRequest>
{
    public OvertimeByIdWithEmployeeSpecification(Guid id) : base(r => r.Id == id)
    {
        AddInclude(r => r.Employee);
    }
}

public sealed class ActiveOvertimeByEmployeeDateSpecification : BaseSpecfication<OvertimeRequest>
{
    private static readonly ApprovalStatus[] Active = [ApprovalStatus.Pending, ApprovalStatus.Approved];

    public ActiveOvertimeByEmployeeDateSpecification(Guid employeeId, DateOnly date)
        : base(r => r.EmployeeId == employeeId && r.Date == date && Active.Contains(r.Status)) { }
}