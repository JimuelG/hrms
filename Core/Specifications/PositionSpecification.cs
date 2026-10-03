using Core.Entities;

namespace Core.Specifications;
public sealed class PositionSearchSpecification : BaseSpecfication<Position>
{
    public PositionSearchSpecification(string? search)
        : base(p => string.IsNullOrEmpty(search) || p.Title.Contains(search) || p.Code.Contains(search))
    {
        AddOrderBy(p => p.Title);
    }
}

public sealed class PositionByCodeSpecification : BaseSpecfication<Position>
{
    public PositionByCodeSpecification(string code, Guid? excludeId = null)
        : base(p => p.Code == code && (excludeId == null || p.Id != excludeId))
    {
        
    }
}