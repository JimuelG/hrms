using Core.Entities;

namespace Core.Specifications;

public sealed class TimelineByEmployeeSpecification : BaseSpecfication<EmployeeTimelineEvent>
{
    public TimelineByEmployeeSpecification(Guid employeeId) 
        : base(e => e.EmployeeId == employeeId)
    {
        AddOrderByDescending(e => e.EventDateUtc);
    }
}