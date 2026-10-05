using Core.Entities;
using Core.Enums;

namespace Core.Specifications;
public sealed class EmployeeSearchSpecification : BaseSpecfication<Employee>
{
    public EmployeeSearchSpecification(string? search)
        : base(e => string.IsNullOrEmpty(search)
            || e.FirstName.Contains(search)
            || e.LastName.Contains(search)
            || e.EmployeeNumber.Contains(search)
            || e.Email.Contains(search)
        )
    {
        AddInclude(e => e.Branch);
        AddInclude(e => e.Department);
        AddInclude(e => e.Position);
        AddInclude(e => e.Manager!);
    }
}

public sealed class EmployeeWithRelationsByIdSpecification : BaseSpecfication<Employee>
{
    public EmployeeWithRelationsByIdSpecification(Guid id): base(e => e.Id == id)
    {
        AddInclude(e => e.Branch);
        AddInclude(e => e.Department);
        AddInclude(e => e.Position);
        AddInclude(e => e.Manager!);
    }
}

public sealed class EmployeeByNumberSpecification : BaseSpecfication<Employee>
{
    public EmployeeByNumberSpecification(string employeeNumber, Guid? excludeId = null)
        : base(e => e.EmployeeNumber == employeeNumber && (excludeId == null || e.Id != excludeId))
    {
        
    }
}

public sealed class EmployeeByEmailSpecification : BaseSpecfication<Employee>
{
    public EmployeeByEmailSpecification(string email, Guid? exludeId = null)
        : base(e => e.Email == email && (exludeId == null || e.Id != exludeId))
    {
        
    }
}

public sealed class EmployeesByManagerSpecification : BaseSpecfication<Employee>
{
    public EmployeesByManagerSpecification(Guid managerId) : base(e => e.ManagerId == managerId) { }
}

public sealed class EligibleManagersSpecification : BaseSpecfication<Employee>
{
    private static readonly EmployeeStatus[] Excluded = [EmployeeStatus.Regular, EmployeeStatus.Terminated, EmployeeStatus.Archived];

    public EligibleManagersSpecification(Guid? excludeEmployeeId = null)
        : base(e => e.Position.IsManagerial
            && !Excluded.Contains(e.Status)
            && (excludeEmployeeId == null || e.Id != excludeEmployeeId))
    {
        AddInclude(e => e.Position);
        AddOrderBy(e => e.LastName);
    }
}