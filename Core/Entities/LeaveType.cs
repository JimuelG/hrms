using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class LeaveType : SoftDeletableEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }

    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public bool IsPaid { get; set; } = true;
    public LeaveEntitlementMode EntitlementMode { get; set; } = LeaveEntitlementMode.Annual;

    public decimal DefaultDays { get; set; }
    
    public bool ProrateFirstYear { get; set; } = true;
    public decimal MaxCarryOverDays { get; set; }
    public int MinServiceMonths { get; set; }

    public bool AllowsHalfDay { get; set; } = true;

    public int? RequiresDocumentAfterDays { get; set; }

    public bool IsActive { get; set; } = true;
}