using Core.Common;
using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;

/// One version of a contribution or tax table. A rate change is a new version with a later EffectiveFrom, and
/// payroll uses the version in force at the period end. Soft-deletable so a mistaken table can be removed;
/// finalized payroll runs snapshot their results, so removing one never changes a past payslip.
public class StatutoryRuleSet : SoftDeletableEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }

    public StatutoryKind Kind { get; set; }
    public StatutoryMethod Method { get; set; }
    public DateOnly EffectiveFrom { get; set; }

    /// Withholding tax only: whether the brackets are for a monthly or a semi-monthly amount.
    public PayFrequency? Basis { get; set; }

    // Rate method only
    public decimal EmployeeRate { get; set; }
    public decimal EmployerRate { get; set; }
    public decimal? BaseFloor { get; set; }
    public decimal? BaseCeiling { get; set; }

    public string Source { get; set; } = default!;     // for example the circular or notice the figures came from
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
    public decimal? ToAmount { get; set; }           // exclusive; null means open-ended
    public decimal EmployeeFixed { get; set; }
    public decimal EmployerFixed { get; set; }
    public decimal EmployeeRateOnExcess { get; set; }
}