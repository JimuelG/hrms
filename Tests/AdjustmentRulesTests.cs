using Core.Common;

namespace Tests;

public class AdjustmentRulesTests
{
    private static readonly PayPeriod P = new(new(2026, 10, 1), new(2026, 10, 15));
    private static DateOnly D(int m, int d) => new(2026, m, d);

    [Fact] public void Taxable_Earnings_Add_And_Taxable_Deductions_Subtract()
    {
        var effect = AdjustmentRules.TaxableEffect([
            new("Bonus", 5000m, AdjustmentTax.Taxable), new("Recovery", -500m, AdjustmentTax.Taxable),
            new("Allowance", 1000m)]);
        Assert.Equal(4500m, effect);
    }

    [Fact] public void Non_Taxable_Adjustments_Have_No_Effect() =>
        Assert.Equal(0m, AdjustmentRules.TaxableEffect([new("Allowance", 1000m), new("Loan", -300m)]));

    [Theory]
    [InlineData(10, 1, null, true, true)]       // starts in the period, open-ended
    [InlineData(10, 16, null, true, false)]     // starts after the period
    [InlineData(1, 1, 9, true, false)]          // ended before the period
    [InlineData(1, 1, 10, true, true)]          // ends inside the period
    [InlineData(1, 1, null, false, false)]      // inactive
    public void A_Rule_Applies_When_Active_And_Overlapping(int fm, int fd, int? tm, bool active, bool expected)
    {
        DateOnly? to = tm is int m ? D(m, tm == 10 ? 5 : 30) : null;
        Assert.Equal(expected, RecurringRules.Applies(D(fm, fd), to, active, P));
    }

    [Fact] public void An_Instalment_Never_Exceeds_The_Balance()
    {
        Assert.Equal(500m, RecurringRules.LoanInstalment(-500m, new LoanState(2000m, 0m)));
        Assert.Equal(200m, RecurringRules.LoanInstalment(-500m, new LoanState(2000m, 1800m)));
        Assert.Equal(0m, RecurringRules.LoanInstalment(-500m, new LoanState(2000m, 2000m)));
    }

    [Fact] public void A_Loan_Balance_Is_Never_Negative() =>
        Assert.Equal(0m, new LoanState(1000m, 1500m).Balance);

    [Fact] public void Rule_Validation_Catches_The_Obvious_Mistakes()
    {
        Assert.Contains("label", RecurringRules.CheckRule(" ", 10m, D(1, 1), null, null));
        Assert.Contains("zero", RecurringRules.CheckRule("X", 0m, D(1, 1), null, null));
        Assert.Contains("end date", RecurringRules.CheckRule("X", 10m, D(2, 1), D(1, 1), null));
        Assert.Contains("negative", RecurringRules.CheckRule("Loan", 100m, D(1, 1), null, 1000m));
        Assert.Contains("exceed", RecurringRules.CheckRule("Loan", -2000m, D(1, 1), null, 1000m));
        Assert.Null(RecurringRules.CheckRule("Loan", -500m, D(1, 1), null, 1000m));
    }

    [Fact] public void Finalize_Allows_Up_To_The_Total_Owed() =>
    Assert.Null(RecurringRules.CheckFinalizeApplication("Loan", 2000m, 1500m, 500m));

[Fact] public void Finalize_Refuses_To_Over_Collect() =>
    Assert.Contains("Recompute", RecurringRules.CheckFinalizeApplication("Loan", 2000m, 1600m, 500m));
}