using Core.Common;
using Core.Interfaces;

namespace Core.Entities;
public class RecurringPayrollItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid EmployeeId { get; set; }

    public string Label { get; set; } = default!;
    public decimal Amount { get; set; }
    public AdjustmentTax Tax { get; set; } = AdjustmentTax.NonTaxable;

    public DateOnly From { get; set; }
    public DateOnly? To { get; set; }
    public bool IsActive { get; set; } = true;

    public decimal? TotalOwed { get; set; }
    public decimal PaidSoFar { get; set; }

    public Guid CreatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = default!;
}

public class LoanRepayment : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid RecurringPayrollItemId { get; set; }
    public Guid PayrollRunId { get; set; }
    public decimal Amount { get; set; }
}

public class PayrollLoanApplication : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid PayrollRunId { get; set; }
    public Guid RecurringPayrollItemId { get; set; }
    public decimal Amount { get; set; }
}