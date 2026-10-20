using Core.Common;
using Core.Enums;

namespace Tests;

public class PayrollComposerTests
{
    private static readonly DateOnly Eff = new(2000, 1, 1);
    private static readonly PayPeriod FirstHalf = new(new(2026, 10, 1), new(2026, 10, 15));
    private static readonly PayPeriod SecondHalf = new(new(2026, 10, 16), new(2026, 10, 31));
    private static readonly PayPeriod WholeMonth = new(new(2026, 10, 1), new(2026, 10, 31));

    private static RuleSetData Brackets(StatutoryKind kind, PayFrequency? basis, params BracketRow[] rows) =>
        new(kind, StatutoryMethod.Brackets, Eff, basis, 0m, 0m, null, null, rows);

    private static RuleSetData Tax(PayFrequency basis) => Brackets(StatutoryKind.WithholdingTax, basis,
        new(0m, 10000m, 0m, 0m, 0m), new(10000m, 30000m, 0m, 0m, 0.10m), new(30000m, null, 2000m, 0m, 0.20m));

    private static InForceRules Rules(PayFrequency taxBasis = PayFrequency.SemiMonthly) => new(
        Brackets(StatutoryKind.Sss, null,
            new(0m, 5000m, 100m, 200m, 0m), new(5000m, 10000m, 200m, 400m, 0m), new(10000m, null, 300m, 600m, 0m)),
        new(StatutoryKind.PhilHealth, StatutoryMethod.Rate, Eff, null, 0.025m, 0.025m, 10000m, 100000m, []),
        new(StatutoryKind.PagIbig, StatutoryMethod.Rate, Eff, null, 0.02m, 0.02m, null, 5000m, []),
        Tax(taxBasis));

    private static readonly InForceRules NoRules = new(null, null, null, null);

    private static PayrollOptions Options(
        PayFrequency f = PayFrequency.SemiMonthly, ContributionDeduction p = ContributionDeduction.SplitEvenly) =>
        new(new PayrollSettings(Frequency: f), p);

    private static PayrollEmployeeInput Input(
        PayPeriod? period = null, decimal unpaid = 0m, params PayrollAdjustment[] adjustments) =>
        new(new GrossInput([new SalaryPoint(new(2020, 1, 1), 26000m)], new(2020, 1, 1), null,
            period ?? FirstHalf, 5, unpaid, []), adjustments);

    private static decimal Line(PayrollResult r, string code) => r.Lines.Single(l => l.Code == code).Amount;

    [Fact] public void First_Half_Applies_Half_Of_Each_Monthly_Contribution_Then_Tax_On_The_Rest()
    {
        var r = PayrollComposer.Compose(Input(), Options(), Rules());

        Assert.Null(r.Error);
        Assert.Equal(13000m, r.Gross);
        Assert.Equal(150m, Line(r, "SSS"));
        Assert.Equal(325m, Line(r, "PHILHEALTH"));
        Assert.Equal(50m, Line(r, "PAGIBIG"));
        Assert.Equal(12475m, r.TaxableIncome);      // 13,000 minus 525 of employee contributions
        Assert.Equal(247.50m, Line(r, "TAX"));      // (12,475 - 10,000) x 10%
        Assert.Equal(12227.50m, r.Net);
        Assert.Equal(675m, r.EmployerContributions);
    }

    [Fact] public void The_Second_Half_Takes_The_Other_Half()
    {
        var r = PayrollComposer.Compose(Input(SecondHalf), Options(), Rules());
        Assert.Equal(150m, Line(r, "SSS"));
        Assert.Equal(12227.50m, r.Net);
    }

    [Fact] public void Second_Period_Only_Takes_Nothing_In_The_First_Half()
    {
        var r = PayrollComposer.Compose(Input(), Options(p: ContributionDeduction.SecondPeriodOnly), Rules());

        Assert.DoesNotContain(r.Lines, l => l.Code is "SSS" or "PHILHEALTH" or "PAGIBIG");
        Assert.Equal(13000m, r.TaxableIncome);
        Assert.Equal(300m, Line(r, "TAX"));          // (13,000 - 10,000) x 10%
        Assert.Equal(12700m, r.Net);
        Assert.Equal(0m, r.EmployerContributions);
    }

    [Fact] public void Second_Period_Only_Takes_It_All_In_The_Second_Half()
    {
        var r = PayrollComposer.Compose(Input(SecondHalf), Options(p: ContributionDeduction.SecondPeriodOnly), Rules());

        Assert.Equal(300m, Line(r, "SSS"));
        Assert.Equal(650m, Line(r, "PHILHEALTH"));
        Assert.Equal(100m, Line(r, "PAGIBIG"));
        Assert.Equal(11950m, r.TaxableIncome);       // 13,000 - 1,050
        Assert.Equal(195m, Line(r, "TAX"));
        Assert.Equal(11755m, r.Net);
        Assert.Equal(1350m, r.EmployerContributions);
    }

    [Fact] public void Monthly_Pay_Takes_The_Whole_Contribution_At_Once()
    {
        var r = PayrollComposer.Compose(Input(WholeMonth), Options(PayFrequency.Monthly), Rules(PayFrequency.Monthly));

        Assert.Equal(26000m, r.Gross);
        Assert.Equal(300m, Line(r, "SSS"));
        Assert.Equal(24950m, r.TaxableIncome);
        Assert.Equal(1495m, Line(r, "TAX"));         // (24,950 - 10,000) x 10%
        Assert.Equal(23455m, r.Net);
    }

    [Fact] public void Unpaid_Leave_Lowers_Gross_And_So_Taxable_Income()
    {
        var r = PayrollComposer.Compose(Input(unpaid: 2m), Options(), Rules());

        Assert.Equal(10609.20m, r.Gross);            // 13,000 - 2 x 1,195.40
        Assert.Equal(10084.20m, r.TaxableIncome);
        Assert.Equal(8.42m, Line(r, "TAX"));
        Assert.Equal(10075.78m, r.Net);
    }

    [Fact] public void Contributions_Come_From_The_Monthly_Basic_Not_The_Reduced_Period_Pay()
    {
        var full = PayrollComposer.Compose(Input(), Options(), Rules());
        var withLeave = PayrollComposer.Compose(Input(unpaid: 5m), Options(), Rules());
        Assert.Equal(Line(full, "SSS"), Line(withLeave, "SSS"));
    }

    [Fact] public void No_Tables_Means_No_Deductions_But_One_Warning_Each()
    {
        var r = PayrollComposer.Compose(Input(), Options(), NoRules);

        Assert.Null(r.Error);
        Assert.Equal(13000m, r.Net);
        Assert.Equal(4, r.Warnings.Count);
        Assert.Contains(r.Warnings, w => w.Contains("SSS"));
        Assert.Contains(r.Warnings, w => w.Contains("withholding tax"));
    }

    [Fact] public void A_Positive_Adjustment_Adds_An_Earning_Line()
    {
        var r = PayrollComposer.Compose(Input(adjustments: new PayrollAdjustment("Allowance", 500m)), Options(), NoRules);

        var line = r.Lines.Single(l => l.Label == "Allowance");
        Assert.Equal(PayrollLineKind.Earning, line.Kind);
        Assert.Equal(500m, line.Amount);
        Assert.Equal(13500m, r.Net);
    }

    [Fact] public void A_Negative_Adjustment_Becomes_A_Deduction_Shown_As_A_Positive_Amount()
    {
        var r = PayrollComposer.Compose(Input(adjustments: new PayrollAdjustment("Cash advance", -200m)), Options(), NoRules);

        var line = r.Lines.Single(l => l.Label == "Cash advance");
        Assert.Equal(PayrollLineKind.Deduction, line.Kind);
        Assert.Equal(200m, line.Amount);
        Assert.Equal(12800m, r.Net);
    }

    [Fact] public void Adjustments_Do_Not_Change_Taxable_Income()
    {
        var plain = PayrollComposer.Compose(Input(), Options(), Rules());
        var adjusted = PayrollComposer.Compose(Input(adjustments: new PayrollAdjustment("Bonus", 5000m)), Options(), Rules());

        Assert.Equal(plain.TaxableIncome, adjusted.TaxableIncome);
        Assert.Contains(adjusted.Notes, n => n.Contains("taxable income"));
    }

    [Fact] public void A_Blank_Label_Or_A_Zero_Amount_Is_Refused()
    {
        Assert.NotNull(PayrollComposer.Compose(Input(adjustments: new PayrollAdjustment("  ", 10m)), Options(), NoRules).Error);
        Assert.NotNull(PayrollComposer.Compose(Input(adjustments: new PayrollAdjustment("Nothing", 0m)), Options(), NoRules).Error);
    }

    [Fact] public void Net_Pay_Below_Zero_Is_Kept_And_Warned_About()
    {
        var r = PayrollComposer.Compose(Input(adjustments: new PayrollAdjustment("Big recovery", -20000m)), Options(), NoRules);

        Assert.Equal(-7000m, r.Net);
        Assert.Contains(r.Warnings, w => w.Contains("negative"));
    }

    [Fact] public void A_Tax_Table_For_The_Wrong_Frequency_Is_Refused() =>
        Assert.Contains("basis", PayrollComposer.Compose(Input(), Options(), Rules(PayFrequency.Monthly)).Error);

    [Fact] public void A_Gross_Calculation_Error_Is_Passed_Through()
    {
        var notEmployed = new PayrollEmployeeInput(
            new GrossInput([new SalaryPoint(new(2020, 1, 1), 26000m)], new(2026, 12, 1), null, FirstHalf, 5, 0m, []), []);
        Assert.NotNull(PayrollComposer.Compose(notEmployed, Options(), Rules()).Error);
    }

    [Fact] public void Net_Is_Exactly_Earnings_Minus_Deductions_As_Printed()
    {
        var r = PayrollComposer.Compose(
            Input(unpaid: 1.5m, adjustments: [new PayrollAdjustment("Allowance", 333.33m), new PayrollAdjustment("Loan", -111.11m)]),
            Options(), Rules());

        Assert.Equal(r.Lines.Where(l => l.Kind == PayrollLineKind.Earning).Sum(l => l.Amount), r.TotalEarnings);
        Assert.Equal(r.Lines.Where(l => l.Kind == PayrollLineKind.Deduction).Sum(l => l.Amount), r.TotalDeductions);
        Assert.Equal(r.TotalEarnings - r.TotalDeductions, r.Net);
    }

    [Fact] public void Every_Line_Is_Rounded_To_Cents()
    {
        var r = PayrollComposer.Compose(Input(unpaid: 0.5m), Options(), Rules());
        Assert.All(r.Lines, l => Assert.Equal(decimal.Round(l.Amount, 2), l.Amount));
    }

    private static PayrollEmployeeInput HiredOct8() =>
    new(new GrossInput([new SalaryPoint(new(2020, 1, 1), 26000m)], new(2026, 10, 8), null, FirstHalf, 5, 0m, []), []);

    private static PayrollOptions Partial(PartialMonthContribution p) =>
        new(new PayrollSettings(PartialMonth: p), ContributionDeduction.SplitEvenly);

    [Fact] public void Full_Period_Policy_Keeps_Todays_Behavior_For_A_Partial_Hire()
    {
        var r = PayrollComposer.Compose(HiredOct8(), Partial(PartialMonthContribution.FullPeriod), Rules());
        Assert.Equal(150m, Line(r, "SSS"));
        Assert.Equal(325m, Line(r, "PHILHEALTH"));
        Assert.DoesNotContain(r.Notes, n => n.Contains("prorated to"));
    }

    [Fact] public void By_Days_Employed_Prorates_Employee_And_Employer_Shares()
    {
        var r = PayrollComposer.Compose(HiredOct8(), Partial(PartialMonthContribution.ByDaysEmployed), Rules());

        Assert.Equal(80m, Line(r, "SSS"));            // 150 x 8/15
        Assert.Equal(173.33m, Line(r, "PHILHEALTH")); // 325 x 8/15 = 173.333
        Assert.Equal(26.67m, Line(r, "PAGIBIG"));     // 50 x 8/15 = 26.667
        Assert.Equal(360m, r.EmployerContributions);  // 160 + 173.33 + 26.67
        Assert.Contains(r.Notes, n => n.Contains("prorated to"));
    }

    [Fact] public void Skip_If_Partial_Takes_No_Contributions_And_Tax_Rises_Accordingly()
    {
        var r = PayrollComposer.Compose(HiredOct8(), Partial(PartialMonthContribution.SkipIfPartial), Rules());

        Assert.DoesNotContain(r.Lines, l => l.Code is "SSS" or "PHILHEALTH" or "PAGIBIG");
        Assert.Equal(0m, r.EmployerContributions);
        Assert.Equal(r.Gross, r.TaxableIncome);
        Assert.Contains(r.Notes, n => n.Contains("wasn't employed for the whole period"));
    }

    [Fact] public void A_Full_Period_Employee_Is_Unaffected_By_Any_Policy()
    {
        var plain = PayrollComposer.Compose(Input(), Options(), Rules());
        foreach (var p in Enum.GetValues<PartialMonthContribution>())
        {
            var r = PayrollComposer.Compose(Input(), Partial(p), Rules());
            Assert.Equal(plain.Net, r.Net);
            Assert.Equal(plain.EmployerContributions, r.EmployerContributions);
        }
    }

    [Fact] public void A_Leaver_Is_Prorated_The_Same_Way()
    {
        var leaver = new PayrollEmployeeInput(
            new GrossInput([new SalaryPoint(new(2020, 1, 1), 26000m)], new(2020, 1, 1), new(2026, 10, 5), FirstHalf, 5, 0m, []), []);

        var r = PayrollComposer.Compose(leaver, Partial(PartialMonthContribution.ByDaysEmployed), Rules());
        Assert.Equal(50m, Line(r, "SSS"));            // 150 x 5/15
    }

    [Fact] public void A_Taxable_Bonus_Raises_Taxable_Income_And_Tax()
    {
        var plain = PayrollComposer.Compose(Input(), Options(), Rules());
        var withBonus = PayrollComposer.Compose(
            Input(adjustments: new PayrollAdjustment("Bonus", 5000m, AdjustmentTax.Taxable)), Options(), Rules());

        Assert.Equal(plain.TaxableIncome + 5000m, withBonus.TaxableIncome);
        Assert.Equal(747.50m, Line(withBonus, "TAX"));      // (17,475 - 10,000) x 10%
        Assert.Contains(withBonus.Notes, n => n.Contains("Taxable adjustments"));
    }

    [Fact] public void A_Non_Taxable_Allowance_Leaves_Tax_Alone()
    {
        var plain = PayrollComposer.Compose(Input(), Options(), Rules());
        var r = PayrollComposer.Compose(Input(adjustments: new PayrollAdjustment("Allowance", 5000m)), Options(), Rules());
        Assert.Equal(Line(plain, "TAX"), Line(r, "TAX"));
    }
}