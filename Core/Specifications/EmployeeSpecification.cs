using Core.Entities;

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