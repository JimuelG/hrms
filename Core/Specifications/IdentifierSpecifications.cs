using Core.Entities;

namespace Core.Specifications;
public sealed class IdentifiersByEmployeeSpecification : BaseSpecfication<EmployeeIdentifiers>
{
    public IdentifiersByEmployeeSpecification(Guid employeeId) : base(i => i.EmployeeId == employeeId) { }
}

public sealed class IdentifierAccessByEmployeeSpecification : BaseSpecfication<IdentifierAccessLog>
{
    public IdentifierAccessByEmployeeSpecification(Guid employeeId) : base(l => l.EmployeeId == employeeId)
    {
        AddOrderByDescending(l => l.CreatedAtUtc);
    }
}