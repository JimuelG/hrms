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
    public decimal? RestDayOvertimeMultiplier { get; set; }
    public decimal? HolidayOvertimeMultiplier { get; set; }
    public bool AttendanceDeductionsEnabled { get; set; } = false;
    public int AttendanceGraceMinutes { get; set; } = 0;
    public TaxMethod TaxMethod { get; set; } = TaxMethod.PerPeriod;
    public PartialMonthContribution PartialMonthContribution { get; set; } = PartialMonthContribution.FullPeriod;
    public decimal? ThirteenthMonthExemptCeiling { get; set; }

    public PayrollSettings ToSettings() => new(Frequency, WorkingDaysFactorFiveDay, WorkingDaysFactorSixDay, HoursPerDay, OrdinaryOvertimeMultiplier, RestDayOvertimeMultiplier, HolidayOvertimeMultiplier, AttendanceDeductionsEnabled, AttendanceGraceMinutes, PartialMonthContribution, TaxMethod);
}