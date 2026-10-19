using Core.Common;
using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class StatutoryRuleSet : SoftDeletableEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }

    public StatutoryKind Kind { get; set; }
    public StatutoryMethod Method { get; set; }
    public DateOnly EffectiveFrom { get; set; }

    public PayFrequency? Basis { get; set; }

    public decimal EmployeeRate { get; set; }
    public decimal EmployerRate { get; set; }
    public decimal? BaseFloor { get; set; }
    public decimal? BaseCeiling { get; set; }

    public string Source { get; set; } = default!;
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }

    public ICollection<StatutoryBracket> Brackets { get; set; } = [];
}

public class StatutoryBracket : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid StatutoryRuleSetId { get; set; }
    public StatutoryRuleSet StatutoryRuleSet { get; set; } = default!;

    public int Order { get; set; }
    public decimal FromAmount { get; set; }
    public decimal? ToAmount { get; set; }
    public decimal EmployeeFixed { get; set; }
    public decimal EmployerFixed { get; set; }
    public decimal EmployeeRateOnExcess { get; set; }

}