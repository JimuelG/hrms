using Core.Entities;
using Core.Enums;

namespace Core.Specifications;
public sealed class OpenAttendanceByEmployeeSpecificaton : BaseSpecfication<AttendanceRecord>
{
    public OpenAttendanceByEmployeeSpecificaton(Guid employeeId) : base(a => a.EmployeeId == employeeId && a.ClockOutUtc == null)
    {
        AddOrderByDescending(a => a.ClockInUtc);
    }
}

public sealed class AttendanceByEmployeeAndDateSpecification : BaseSpecfication<AttendanceRecord>
{
    public AttendanceByEmployeeAndDateSpecification(Guid employeeId, DateOnly date) : base(a => a.EmployeeId == employeeId && a.Date == date) {}
}

public sealed class AttendanceByIdWithEmployeeSpecification : BaseSpecfication<AttendanceRecord>
{
    public AttendanceByIdWithEmployeeSpecification(Guid id) : base(a => a.Id == id)
    {
        AddInclude(a => a.Employee);
    }
}

public sealed class AttendanceByEmployeeRangeSpecification : BaseSpecfication<AttendanceRecord>
{
    public AttendanceByEmployeeRangeSpecification(Guid employeeId, DateOnly from, DateOnly to)
        : base(a => a.EmployeeId == employeeId && ((a.Date >= from && a.Date <= to) || a.ClockOutUtc == null))
    {
        AddInclude(a => a.Employee);
        AddOrderByDescending(a => a.ClockInUtc);
    }
}

public sealed class AttendanceQuerySpecification : BaseSpecfication<AttendanceRecord>
{
    public AttendanceQuerySpecification(
        DateOnly from, DateOnly to, Guid? employeeId, bool needsReview, int? skip = null, int? take = null)
        : base(a => a.Date >= from && a.Date <= to
            && (employeeId == null || a.EmployeeId == employeeId)
            && (!needsReview || a.Flags != AttendanceFlags.None))
    {
        AddInclude(a => a.Employee);
        AddOrderByDescending(a => a.ClockInUtc);
        if (skip is not null && take is not null)
            ApplyPaging(skip.Value, take.Value);
    }
}