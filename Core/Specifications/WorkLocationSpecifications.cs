using Core.Entities;
using Core.Enums;

namespace Core.Specifications;
public sealed class WorkLocationSearchSpecification : BaseSpecfication<WorkLocation>
{
    public WorkLocationSearchSpecification(string? search, WorkLocationType? type)
        : base(l => (string.IsNullOrEmpty(search) || l.Name.Contains(search)) && (type == null || l.Type == type))
    {
        AddInclude(l => l.Branch!);
    }
}

public sealed class WorkLocationByIdWithBranchSpefication : BaseSpecfication<WorkLocation>
{
    public WorkLocationByIdWithBranchSpefication(Guid id) : base(l => l.Id == id)
    {
        AddInclude(l => l.Branch!);
    }
}

public sealed class WorkLocationsByIdsSpecification : BaseSpecfication<WorkLocation>
{
    public WorkLocationsByIdsSpecification(List<Guid> ids) : base(l => ids.Contains(l.Id))
    {
        AddInclude(l => l.Branch!);
    }
}

public sealed class WorkLocationByNameSpecification : BaseSpecfication<WorkLocation>
{
    public WorkLocationByNameSpecification(string name, Guid? excludeId = null) : base(l => l.Name == name && (excludeId == null || l.Id != excludeId)) { }
}

public sealed class AssignmentsByIdEmployeeSpecification : BaseSpecfication<EmployeeWorkLocation>
{
    public AssignmentsByIdEmployeeSpecification(Guid employeeId) : base(a => a.EmployeeId == employeeId) {}
}