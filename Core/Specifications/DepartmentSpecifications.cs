using Core.Entities;

namespace Core.Specifications;

public sealed class DepartmentSearchSpecification : BaseSpecfication<Department>
{
    public DepartmentSearchSpecification(string? search)
        : base(d => string.IsNullOrEmpty(search) || d.Name.Contains(search) || d.Code.Contains(search))
    {
        AddOrderBy(d => d.Name);
    }

}

public sealed class DepartmentByCodeSpecification : BaseSpecfication<Department>
{
    public DepartmentByCodeSpecification(string code, Guid? excludeId = null)
        : base (d => d.Code == code && (excludeId == null || d.Id != excludeId))
    {
        
    }
}