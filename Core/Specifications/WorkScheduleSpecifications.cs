using Core.Entities;

namespace Core.Specifications;
public sealed class WorkScheduleSearchSpecification : BaseSpecfication<WorkSchedule>
{
    public WorkScheduleSearchSpecification(string? search)
        : base(s => string.IsNullOrEmpty(search) || s.Name.Contains(search))
    {
        AddOrderBy(s => s.Name);
    }
}

public sealed class WorkScheduleByNameSpecification : BaseSpecfication<WorkSchedule>
{
    public WorkScheduleByNameSpecification(string name, Guid? excludeId = null)
        : base(s => s.Name == name && (excludeId == null || s.Id != excludeId))
    {
        
    }
}