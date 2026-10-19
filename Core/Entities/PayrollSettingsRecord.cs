using Core.Common;
using Core.Interfaces;

namespace Core.Entities;
public class PayrollSettingsRecord : BaseEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }

    public PayFrequency Frequency { get; set; } = PayFrequency.SemiMonthly;
    public ContributionDeduction ContributionPolicy { get; set; } = ContributionDeduction.SplitEvenly;
    public int WorkingDaysFactorFiveDay { get; set; } = 261;
    public int WorkingDaysFactorSixDay { get; set; } = 313;
    public decimal HoursPerDay { get; set; } = 8m;
    public decimal OrdinaryOvertimeMultiplier { get; set; } = 1.25m;

    public PayrollSettings ToSettings() => new(Frequency, WorkingDaysFactorFiveDay, WorkingDaysFactorSixDay, HoursPerDay, OrdinaryOvertimeMultiplier);
}