using Core.Common;
using Core.Enums;

namespace Tests;

public class ThirteenthMonthTests
{
    private static ThirteenthAmount Calc(decimal earned, decimal paid = 0m, decimal used = 0m, decimal? ceiling = null) =>
        ThirteenthMonthCalculator.Calculate(new(earned, paid, used, ceiling));

    // ---- entitlement ----
    [Fact] public void A_Twelfth_Of_Basic_Earned_Is_The_Entitlement()
    {
        var a = Calc(312000m);
        Assert.Equal(26000m, a.Entitlement);
        Assert.Equal(26000m, a.Amount);
    }

    [Fact] public void The_Entitlement_Is_Rounded_To_Cents() => Assert.Equal(8333.33m, Calc(100000m).Entitlement);

    [Fact] public void A_Mid_Year_Advance_Then_The_Balance_Adds_Up_To_The_Entitlement()
    {
        var advance = Calc(156000m);                         // earned to June 30
        var balance = Calc(312000m, paid: advance.Amount);   // earned to December 31
        Assert.Equal(13000m, advance.Amount);
        Assert.Equal(13000m, balance.Amount);
        Assert.Equal(26000m, advance.Amount + balance.Amount);
    }

    [Fact] public void Paying_More_Than_Entitled_Gives_Zero_And_A_Warning()
    {
        var a = Calc(120000m, paid: 12000m);                 // entitlement 10,000
        Assert.Equal(0m, a.Amount);
        Assert.Contains(a.Warnings, w => w.Contains("more than"));
    }

    [Fact] public void Negative_Earnings_Count_As_Nothing() => Assert.Equal(0m, Calc(-500m).Entitlement);

    // ---- tax-exempt part ----
    [Fact] public void An_Amount_Under_The_Ceiling_Is_Fully_Exempt()
    {
        var a = Calc(312000m, ceiling: 90000m);
        Assert.Equal((26000m, 0m), (a.Exempt, a.Taxable));
        Assert.Empty(a.Warnings);
    }

    [Fact] public void The_Excess_Over_The_Ceiling_Is_Taxable()
    {
        var a = Calc(312000m, ceiling: 20000m);
        Assert.Equal((20000m, 6000m), (a.Exempt, a.Taxable));
    }

    [Fact] public void The_Ceiling_Is_Shared_Across_Payments()
    {
        var a = Calc(120000m, used: 15000m, ceiling: 20000m);   // 10,000 due, 5,000 of ceiling left
        Assert.Equal((5000m, 5000m), (a.Exempt, a.Taxable));
    }

    [Fact] public void A_Used_Up_Ceiling_Leaves_Nothing_Exempt()
    {
        var a = Calc(120000m, used: 25000m, ceiling: 20000m);
        Assert.Equal((0m, 10000m), (a.Exempt, a.Taxable));
    }

    [Fact] public void No_Ceiling_Means_All_Taxable_With_A_Warning()
    {
        var a = Calc(312000m);
        Assert.Equal((0m, 26000m), (a.Exempt, a.Taxable));
        Assert.Contains(a.Warnings, w => w.Contains("No tax-exempt ceiling"));
    }

    [Fact] public void Nothing_Due_Gives_No_Ceiling_Warning() => Assert.Empty(Calc(0m).Warnings);

    // ---- what counts as basic earned ----
    [Fact] public void Only_Basic_And_Its_Deductions_Count()
    {
        var lines = new List<PayrollLine>
        {
            new("BASIC", "Basic salary", 13000m, PayrollLineKind.Earning),
            new("UNPAID_LEAVE", "Unpaid leave", -2390.80m, PayrollLineKind.Earning),
            new("ABSENT", "Absences", -500m, PayrollLineKind.Earning),
            new("LATE", "Late", -74.72m, PayrollLineKind.Earning),
            new("UNDERTIME", "Undertime", -10m, PayrollLineKind.Earning),
            new("OVERTIME", "Overtime", 400m, PayrollLineKind.Earning),
            new("ADJUSTMENT", "Bonus", 999m, PayrollLineKind.Earning),
            new("SSS", "SSS", 150m, PayrollLineKind.Deduction),
            new("TAX", "Withholding tax", 50m, PayrollLineKind.Deduction)
        };
        Assert.Equal(10024.48m, ThirteenthMonthRules.BasicEarned(lines));
    }

    // ---- period and sequencing ----
    private static readonly DateOnly Today = new(2026, 12, 10);
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    [Fact] public void Year_End_And_Mid_Year_Cutoffs_Are_Accepted()
    {
        Assert.Null(ThirteenthMonthRules.CheckPeriod(2026, D(2026, 12, 31), Today));
        Assert.Null(ThirteenthMonthRules.CheckPeriod(2026, D(2026, 6, 30), Today));
    }

    [Fact] public void A_Cutoff_That_Is_Not_A_Month_End_Is_Refused() =>
        Assert.Contains("last day of a month", ThirteenthMonthRules.CheckPeriod(2026, D(2026, 6, 15), Today));

    [Fact] public void A_Cutoff_In_Another_Year_Is_Refused() =>
        Assert.Contains("must fall in 2026", ThirteenthMonthRules.CheckPeriod(2026, D(2025, 12, 31), Today));

    [Fact] public void A_Cutoff_More_Than_A_Month_Ahead_Is_Refused() =>
        Assert.Contains("more than a month ahead", ThirteenthMonthRules.CheckPeriod(2026, D(2026, 12, 31), D(2026, 10, 18)));

    [Fact] public void The_Year_Must_Be_In_Range() =>
        Assert.Contains("out of range", ThirteenthMonthRules.CheckPeriod(1999, D(1999, 12, 31), D(1999, 12, 10)));

    [Fact] public void With_Nothing_Existing_Any_Cutoff_Is_Fine() =>
        Assert.Null(ThirteenthMonthRules.CheckExisting([], D(2026, 12, 31)));

    [Fact] public void A_Later_Run_After_A_Finalized_Earlier_One_Is_Allowed() =>
        Assert.Null(ThirteenthMonthRules.CheckExisting([(D(2026, 6, 30), PayrollRunStatus.Finalized)], D(2026, 12, 31)));

    [Fact] public void An_Unfinalized_Earlier_Run_Blocks_A_Later_One() =>
        Assert.Contains("Finalize the earlier",
            ThirteenthMonthRules.CheckExisting([(D(2026, 6, 30), PayrollRunStatus.Draft)], D(2026, 12, 31)));

    [Fact] public void A_Run_That_Already_Covers_The_Cutoff_Blocks_It()
    {
        Assert.Contains("already exists", ThirteenthMonthRules.CheckExisting([(D(2026, 12, 31), PayrollRunStatus.Draft)], D(2026, 12, 31)));
        Assert.Contains("already exists", ThirteenthMonthRules.CheckExisting([(D(2026, 12, 31), PayrollRunStatus.Finalized)], D(2026, 6, 30)));
    }

    // ---- composing the payslip ----
    private static RuleSetData MonthlyTax(PayFrequency basis = PayFrequency.Monthly) =>
        new(StatutoryKind.WithholdingTax, StatutoryMethod.Brackets, new DateOnly(2000, 1, 1), basis, 0m, 0m, null, null,
        [new(0m, 10000m, 0m, 0m, 0m), new(10000m, 30000m, 0m, 0m, 0.10m), new(30000m, null, 2000m, 0m, 0.20m)]);

    private static decimal Line(PayrollResult r, string code) => r.Lines.Single(l => l.Code == code).Amount;

    [Fact] public void Cumulative_Tax_Applies_To_The_Taxable_Part_Against_Year_To_Date()
    {
        var a = Calc(312000m, ceiling: 20000m);   // 26,000 due: 20,000 exempt, 6,000 taxable
        var r = PayrollComposer_Compose(a, [], TaxMethod.Cumulative, MonthlyTax(), new YearToDate(20000m, 1000m));

        Assert.Null(r.Error);
        Assert.Equal(26000m, Line(r, "THIRTEENTH"));
        Assert.Equal(600m, Line(r, "TAX"));          // table(26,000) = 1,600, minus 1,000 withheld
        Assert.Equal(6000m, r.TaxableIncome);
        Assert.Equal(26000m, r.Gross);
        Assert.Equal(25400m, r.Net);
        Assert.Equal(0m, r.EmployerContributions);
    }

    [Fact] public void With_No_Ceiling_And_No_History_Everything_Is_Taxed()
    {
        var r = PayrollComposer_Compose(Calc(312000m), [], TaxMethod.Cumulative, MonthlyTax(), null);
        Assert.Equal(1600m, Line(r, "TAX"));
        Assert.Equal(24400m, r.Net);
        Assert.Contains(r.Warnings, w => w.Contains("No tax-exempt ceiling"));
    }

    [Fact] public void A_Fully_Exempt_Amount_Has_No_Tax_Line()
    {
        var r = PayrollComposer_Compose(Calc(312000m, ceiling: 90000m), [], TaxMethod.Cumulative, MonthlyTax(), null);
        Assert.DoesNotContain(r.Lines, l => l.Code == "TAX");
        Assert.Equal(26000m, r.Net);
        Assert.Empty(r.Warnings);
    }

    [Fact] public void Per_Period_Tax_Records_The_Taxable_Part_But_Withholds_Nothing_And_Warns()
    {
        var r = PayrollComposer_Compose(Calc(312000m, ceiling: 20000m), [], TaxMethod.PerPeriod, MonthlyTax(), null);
        Assert.DoesNotContain(r.Lines, l => l.Code == "TAX");
        Assert.Equal(6000m, r.TaxableIncome);
        Assert.Contains(r.Warnings, w => w.Contains("per-period"));
    }

    [Fact] public void A_Semi_Monthly_Table_Is_Refused_For_Cumulative_Tax() =>
        Assert.NotNull(PayrollComposer_Compose(Calc(312000m), [], TaxMethod.Cumulative, MonthlyTax(PayFrequency.SemiMonthly), null).Error);

    [Fact] public void Adjustments_Add_Lines_And_Do_Not_Touch_Tax()
    {
        var r = PayrollComposer_Compose(Calc(312000m, ceiling: 90000m),
            [new("Allowance", 500m), new("Recovery", -200m)], TaxMethod.Cumulative, MonthlyTax(), null);

        Assert.Equal(PayrollLineKind.Earning, r.Lines.Single(l => l.Label == "Allowance").Kind);
        Assert.Equal(PayrollLineKind.Deduction, r.Lines.Single(l => l.Label == "Recovery").Kind);
        Assert.Equal(26300m, r.Net);
    }

    [Fact] public void A_Blank_Adjustment_Label_Is_Refused() =>
        Assert.NotNull(PayrollComposer_Compose(Calc(312000m), [new("  ", 10m)], TaxMethod.PerPeriod, null, null).Error);

    [Fact] public void Nothing_Due_Gives_An_Empty_Payslip()
    {
        var r = PayrollComposer_Compose(Calc(0m), [], TaxMethod.PerPeriod, null, null);
        Assert.Empty(r.Lines);
        Assert.Equal(0m, r.Net);
    }

    private static PayrollResult PayrollComposer_Compose(
        ThirteenthAmount a, IReadOnlyList<PayrollAdjustment> adj, TaxMethod m, RuleSetData? rule, YearToDate? ytd) =>
        ThirteenthMonthComposer.Compose(a, adj, m, rule, ytd);
}