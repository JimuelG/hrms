using Core.Entities;
using Core.Enums;

namespace Core.Specifications;
public sealed class AttendanceReportEmployeesSpecification : BaseSpecfication<Employee>
{
    private static readonly EmployeeStatus[] NotYet = [EmployeeStatus.Applicant, EmployeeStatus.PreOnboarding];

    public AttendanceReportEmployeesSpecification(DateOnly from, DateOnly to, Guid? departmentId, Guid? branchId)
        : base(e => e.HireDate <= to
                && !NotYet.Contains(e.Status)
                && (e.LastWorkingDay == null || e.LastWorkingDay >= from)
                && (e.LastWorkingDay != null || (e.Status != EmployeeStatus.Resigned && e.Status != EmployeeStatus.Terminated))
                && (departmentId == null || e.DepartmentId == departmentId)
                && (branchId == null || e.BranchId == branchId))
    {
        AddInclude(e => e.Department);
        AddInclude(e => e.Branch);
        AddOrderBy(e => e.LastName);
    }
}

public sealed class LeaveRequestsByYearSpecificaiton : BaseSpecfication<LeaveRequest>
{
    public LeaveRequestsByYearSpecificaiton(int year) : base(r => r.LeaveYear == year && (r.Status == ApprovalStatus.Approved || r.Status == ApprovalStatus.Pending)) 
    {}
}

public sealed class LeaveEntriesByYearSpecification : BaseSpecfication<LeaveBalanceEntry>
{
    public LeaveEntriesByYearSpecification(int year) : base(e => e.Year == year) {}
}