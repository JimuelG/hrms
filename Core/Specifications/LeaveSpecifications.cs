using Core.Entities;
using Core.Enums;

namespace Core.Specifications;
public sealed class LeaveTypeSearchSpecification : BaseSpecfication<LeaveType>
{
    public LeaveTypeSearchSpecification(string? search, bool activeOnly) 
        : base(t => (string.IsNullOrEmpty(search) || t.Name.Contains(search) || t.Code.Contains(search)) && (!activeOnly || t.IsActive))
    {
        AddOrderBy(t => t.Name);
    }
}

public sealed class LeaveTypeByCodeSpecification : BaseSpecfication<LeaveType>
{
    public LeaveTypeByCodeSpecification(string code, Guid? excludeId = null)
        : base(t => t.Code == code && (excludeId == null || t.Id != excludeId)) {}
}

public sealed class HolidaysInRangeSpecification : BaseSpecfication<Holiday>
{
    public HolidaysInRangeSpecification(DateOnly from, DateOnly to) : base(h => h.Date >= from && h.Date <= to)
    {
        AddOrderBy(h => h.Date);
    }
}

public sealed class HolidayByDateSpecification : BaseSpecfication<Holiday>
{
    public HolidayByDateSpecification(DateOnly date, Guid? excludeId = null)
        : base(h => h.Date == date && (excludeId == null || h.Id != excludeId))
    {}
}

public sealed class AnnualLeaveTypesSpecification : BaseSpecfication<LeaveType>
{
    public AnnualLeaveTypesSpecification() : base(t => t.IsActive && t.EntitlementMode == LeaveEntitlementMode.Annual) { }
}

public sealed class LeaveEntriesByEmployeeSpecification : BaseSpecfication<LeaveBalanceEntry>
{
    public LeaveEntriesByEmployeeSpecification(Guid employeeId) : base(e => e.EmployeeId == employeeId)
    {
        AddOrderByDescending(e => e.CreatedAtUtc);
    }
}

public sealed class LeaveEntriesByTypeSpecification : BaseSpecfication<LeaveBalanceEntry>
{
    public LeaveEntriesByTypeSpecification(Guid leaveTypeId) : base(e => e.LeaveTypeId == leaveTypeId) { }
}

public sealed class LeaveEntriesByRequestSpecification : BaseSpecfication<LeaveBalanceEntry>
{
    public LeaveEntriesByRequestSpecification(Guid leaveRequestId) : base(e => e.LeaveRequestId == leaveRequestId) { }
}
