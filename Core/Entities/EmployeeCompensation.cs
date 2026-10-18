using Core.Interfaces;

namespace Core.Entities;
public class EmployeeCompensation : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid EmployeeId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public decimal MonthlyBasicSalary { get; set; }
    public string Reason { get; set; } = default!;
    public Guid CreatedByUserId { get; set; }
}