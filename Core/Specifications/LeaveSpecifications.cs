using Core.Entities;

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
