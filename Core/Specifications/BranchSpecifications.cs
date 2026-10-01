using Core.Entities;

namespace Core.Specifications;
public sealed class BranchSearchSpecification : BaseSpecfication<Branch>
{
    public BranchSearchSpecification(string? search)
        : base(b => string.IsNullOrEmpty(search) || b.Name.Contains(search) || b.Code.Contains(search))
    {
        AddOrderBy(b => b.Name);
    }

}

public sealed class BranchByCodeSpecification : BaseSpecfication<Branch>
{
    public BranchByCodeSpecification(string code, Guid? excludeId = null)
        : base(b => b.Code == code && (excludeId == null || b.Id != excludeId))
    {
        
    }
}