using Core.Entities;
using Core.Enums;

namespace Core.Specifications;

public sealed class WorkingCandidatesSpecification : BaseSpecfication<Employee>
{
    private static readonly EmployeeStatus[] NotYet = [EmployeeStatus.Applicant, EmployeeStatus.PreOnboarding];

    public WorkingCandidatesSpecification(DateOnly today, Guid? managerId)
        : base(e => e.HireDate <= today && !NotYet.Contains(e.Status)
                && (e.LastWorkingDay == null || e.LastWorkingDay >= today)
                && (managerId == null || e.ManagerId == managerId))
    {
        AddOrderBy(e => e.LastName);
    }
}

public sealed class DirectReportsSpecification : BaseSpecfication<Employee>
{
    public DirectReportsSpecification(Guid managerId) : base(e => e.ManagerId == managerId) { }
}

public sealed class HiredBetweenSpecification : BaseSpecfication<Employee>
{
    private static readonly EmployeeStatus[] NotYet = [EmployeeStatus.Applicant, EmployeeStatus.PreOnboarding];
    public HiredBetweenSpecification(DateOnly from, DateOnly to)
        : base(e => e.HireDate >= from && e.HireDate <= to && !NotYet.Contains(e.Status)) { }
}

public sealed class LeftBetweenSpecification : BaseSpecfication<Employee>
{
    public LeftBetweenSpecification(DateOnly from, DateOnly to)
        : base(e => e.LastWorkingDay != null && e.LastWorkingDay >= from && e.LastWorkingDay <= to) {}
}

public sealed class DashboardSchedulesSpecification : BaseSpecfication<WorkSchedule>
{
    public DashboardSchedulesSpecification() : base(s => true) {}
}

public sealed class OpenJobPostingsSpecification : BaseSpecfication<JobPosting>
{
    public OpenJobPostingsSpecification() : base(p => p.Status == JobPostingStatus.Open) {}
}

public sealed class PendingLeaveByEmployeeSpecification : BaseSpecfication<LeaveRequest>
{
    public PendingLeaveByEmployeeSpecification(Guid employeeId)
        : base(r => r.EmployeeId == employeeId && r.Status == ApprovalStatus.Pending) {}
}

public sealed class WorkforceEmployeesSpecification : BaseSpecfication<Employee>
{
    private static readonly EmployeeStatus[] NotYet = [EmployeeStatus.Applicant, EmployeeStatus.PreOnboarding];

    public WorkforceEmployeesSpecification(DateOnly today)
        : base(e => e.HireDate <= today && !NotYet.Contains(e.Status)
                && (e.LastWorkingDay == null || e.LastWorkingDay >= today))
    {
        AddInclude(e => e.Department);
        AddInclude(e => e.Branch);
        AddInclude(e => e.Position);
        AddOrderBy(e => e.LastName);
    }
}