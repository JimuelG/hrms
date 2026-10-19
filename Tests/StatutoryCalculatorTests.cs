using Core.Common;
using Core.Enums;

namespace Tests;

public class StatutoryCalculatorTests
{
    private static readonly DateOnly Eff = new(2026, 1, 1);

    private static RuleSetData Brackets(StatutoryKind kind, params BracketRow[] rows) =>
        new(kind, StatutoryMethod.Brackets, Eff, kind == StatutoryKind.WithholdingTax ? PayFrequency.SemiMonthly : null,
            0m, 0m, null, null, rows);

    private static RuleSetData Rate(decimal ee, decimal er, decimal? floor = null, decimal? ceiling = null) =>
        new(StatutoryKind.PagIbig, StatutoryMethod.Rate, Eff, null, ee, er, floor, ceiling, []);

    // fake SSS-style table
    private static readonly RuleSetData FakeSss = Brackets(StatutoryKind.Sss,
        new(0m, 5000m, 100m, 200m, 0m), new(5000m, 10000m, 200m, 400m, 0m), new(10000m, null, 300m, 600m, 0m));

    // fake tax table: nothing under 10,000, then 10% on the excess, then 2,000 plus 20% over 30,000
    private static readonly RuleSetData FakeTax = Brackets(StatutoryKind.WithholdingTax,
        new(0m, 10000m, 0m, 0m, 0m), new(10000m, 30000m, 0m, 0m, 0.10m), new(30000m, null, 2000m, 0m, 0.20m));

    [Theory]
    [InlineData(1, 100, 200)]
    [InlineData(4999.99, 100, 200)]
    [InlineData(5000, 200, 400)]      // a boundary belongs to the row that starts there
    [InlineData(9999.99, 200, 400)]
    [InlineData(10000, 300, 600)]
    [InlineData(1000000, 300, 600)]   // the open-ended row covers everything above
    public void A_Bracket_Table_Returns_The_Row_The_Amount_Falls_In(double amount, double ee, double er)
    {
        var a = StatutoryCalculator.Compute(FakeSss, (decimal)amount);
        Assert.Equal((decimal)ee, a.Employee);
        Assert.Equal((decimal)er, a.Employer);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Nothing_Is_Due_On_Zero_Or_Negative_Amounts(double amount)
    {
        var a = StatutoryCalculator.Compute(FakeSss, (decimal)amount);
        Assert.Equal(0m, a.Employee);
        Assert.Equal(0m, a.Employer);
    }

    [Theory]
    [InlineData(5000, 0)]
    [InlineData(10000, 0)]
    [InlineData(20000, 1000)]
    [InlineData(30000, 2000)]
    [InlineData(40000, 4000)]
    public void A_Tax_Table_Adds_The_Rate_On_The_Excess_To_The_Base(double income, double tax) =>
        Assert.Equal((decimal)tax, StatutoryCalculator.Compute(FakeTax, (decimal)income).Employee);

    [Fact] public void Tax_Is_Rounded_To_Cents_Away_From_Zero() =>
        Assert.Equal(1555.56m, StatutoryCalculator.Compute(FakeTax, 25555.55m).Employee);   // 15,555.55 x 10% = 1,555.555

    [Fact] public void Tax_Has_No_Employer_Share() =>
        Assert.Equal(0m, StatutoryCalculator.Compute(FakeTax, 40000m).Employer);

    [Theory]
    [InlineData(3000, 60, 60)]
    [InlineData(100, 2, 2)]
    [InlineData(20000, 100, 100)]     // capped at the 5,000 ceiling
    public void A_Rate_Rule_Applies_The_Rate_Up_To_The_Ceiling(double amount, double ee, double er)
    {
        var a = StatutoryCalculator.Compute(Rate(0.02m, 0.02m, ceiling: 5000m), (decimal)amount);
        Assert.Equal((decimal)ee, a.Employee);
        Assert.Equal((decimal)er, a.Employer);
    }

    [Theory]
    [InlineData(8000, 250)]           // under the floor: charged as if it were 10,000
    [InlineData(50000, 1250)]
    [InlineData(500000, 2500)]        // over the ceiling: charged as if it were 100,000
    public void A_Floor_And_Ceiling_Clamp_The_Base(double amount, double each)
    {
        var a = StatutoryCalculator.Compute(Rate(0.025m, 0.025m, 10000m, 100000m), (decimal)amount);
        Assert.Equal((decimal)each, a.Employee);
        Assert.Equal((decimal)each, a.Employer);
    }

    [Fact] public void Rate_Results_Are_Rounded_To_Cents() =>
        Assert.Equal(308.64m, StatutoryCalculator.Compute(Rate(0.025m, 0.025m), 12345.67m).Employee);   // 308.64175

    [Fact] public void Employee_And_Employer_Rates_Can_Differ()
    {
        var a = StatutoryCalculator.Compute(Rate(0.01m, 0.02m), 1000m);
        Assert.Equal(10m, a.Employee);
        Assert.Equal(20m, a.Employer);
    }

    [Fact] public void A_Rate_Rule_Charges_Nothing_On_Zero() =>
        Assert.Equal(0m, StatutoryCalculator.Compute(Rate(0.02m, 0.02m, floor: 1000m), 0m).Employee);   // the floor doesn't invent pay

    [Fact] public void Bracket_Order_In_The_Input_Does_Not_Matter()
    {
        var shuffled = Brackets(StatutoryKind.Sss,
            new(10000m, null, 300m, 600m, 0m), new(0m, 5000m, 100m, 200m, 0m), new(5000m, 10000m, 200m, 400m, 0m));
        Assert.Equal(200m, StatutoryCalculator.Compute(shuffled, 7000m).Employee);
    }
}