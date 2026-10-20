using Core.Common;
using Core.Interfaces;

namespace Core.Entities;
public class PayrollAdjustmentRecord : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid PayrollRunId { get; set; }
    public PayrollRun Run { get; set; } = default!;
    
    public Guid EmployeeId { get; set; }
    public string Label { get; set; } = default!;
    public decimal Amount { get; set; }
    public Guid CreatedByUserId { get; set; }
    public AdjustmentTax Tax { get; set; } = AdjustmentTax.NonTaxable;
}