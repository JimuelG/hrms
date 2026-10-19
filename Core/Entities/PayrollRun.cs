using Core.Common;
using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class PayrollRun : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public PayrollRunStatus Status { get; set; } = PayrollRunStatus.Draft;

    public PayFrequency Frequency { get; set; }
    public ContributionDeduction ContributionPolicy { get; set; }
    public int WorkingDaysFactorFiveDay { get; set; }
    public int WorkingDaysFactorySixDay { get; set; }
    public decimal HoursPerDay { get; set; }
    public decimal OrdinaryOvertimeMultiplier { get; set; }

    public string? Notes { get; set; }
    
    public Guid CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public Guid? FinalizedByUserId { get; set; }
    public DateTime? FinalizedAtUtc { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }

    public DateTime LastActivityAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = default!;

    public ICollection<PayrollResultRecord> Results { get; set; } = [];
    public ICollection<PayrollAdjustmentRecord> Adjustments { get; set; } = [];

    public PayrollSettings ToSettings() => new(Frequency, WorkingDaysFactorFiveDay, WorkingDaysFactorySixDay, HoursPerDay, OrdinaryOvertimeMultiplier);
    public PayPeriod Period => new(PeriodStart, PeriodEnd);
}