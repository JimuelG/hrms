using Core.Common;
using Core.Enums;

namespace Tests;

public class CumulativeTaxTests
{
    [Fact] public void Tax_Is_The_Cumulative_Figure_Minus_What_Was_Already_Withheld() =>
        Assert.Equal(150m, CumulativeTax.ThisPeriod(450m, 300m));

    [Fact] public void An_Over_Withholding_Never_Produces_A_Negative_Tax() =>
        Assert.Equal(0m, CumulativeTax.ThisPeriod(300m, 450m));

    private static readonly DateOnly Eff = new(2000, 1, 1);
    private static readonly PayPeriod Oct = new(new(2026, 10, 1), new(2026, 10, 31));

    private static InForceRules Rules() => new(null, null, null,
        new RuleSetData(StatutoryKind.WithholdingTax, StatutoryMethod.Brackets, Eff, PayFrequency.Monthly, 0m, 0m, null, null,
        [new(0m, 10000m, 0m, 0m, 0m), new(10000m, 30000m, 0m, 0m, 0.10m), new(30000m, null, 2000m, 0m, 0.20m)]));

    private static PayrollEmployeeInput Input(YearToDate? ytd) =>
        new(new GrossInput([new SalaryPoint(new(2020, 1, 1), 26000m)], new(2020, 1, 1), null, Oct, 5, 0m, []), [], ytd);

    private static PayrollOptions Options(TaxMethod m) =>
        new(new PayrollSettings(Frequency: PayFrequency.Monthly, TaxMethod: m), ContributionDeduction.SplitEvenly);

    [Fact] public void Per_Period_Ignores_Year_To_Date()
    {
        var r = PayrollComposer.Compose(Input(new YearToDate(500000m, 99999m)), Options(TaxMethod.PerPeriod), Rules());
        Assert.Equal(1600m, r.Lines.Single(l => l.Code == "TAX").Amount);   // (26,000 - 10,000) x 10%
    }

    [Fact] public void Cumulative_Charges_The_Difference_Against_Year_To_Date()
    {
        // YTD taxable 20,000 with 1,000 withheld; this period 26,000 -> cumulative taxable 46,000
        var r = PayrollComposer.Compose(Input(new YearToDate(20000m, 1000m)), Options(TaxMethod.Cumulative), Rules());
        // table(46,000) = 2,000 + 20% x 16,000 = 5,200; minus 1,000 already withheld = 4,200
        Assert.Equal(4200m, r.Lines.Single(l => l.Code == "TAX").Amount);
        Assert.Contains(r.Notes, n => n.Contains("cumulative"));
    }

    [Fact] public void Cumulative_Without_History_Equals_Per_Period_For_The_First_Period()
    {
        var a = PayrollComposer.Compose(Input(null), Options(TaxMethod.Cumulative), Rules());
        var b = PayrollComposer.Compose(Input(null), Options(TaxMethod.PerPeriod), Rules());
        Assert.Equal(b.Lines.Single(l => l.Code == "TAX").Amount, a.Lines.Single(l => l.Code == "TAX").Amount);
    }

    [Fact] public void Cumulative_Refuses_A_Non_Monthly_Table()
    {
        var wrong = Rules() with { Tax = Rules().Tax! with { Basis = PayFrequency.SemiMonthly } };
        Assert.Contains("basis", PayrollComposer.Compose(Input(null), Options(TaxMethod.Cumulative), wrong).Error);
    }
}