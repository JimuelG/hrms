using Core.Common;

namespace Tests;

public class PartialPeriodRulesTests
{
    [Theory]
    [InlineData(PartialMonthContribution.FullPeriod)]
    [InlineData(PartialMonthContribution.ByDaysEmployed)]
    [InlineData(PartialMonthContribution.SkipIfPartial)]
    public void A_Full_Period_Is_Never_Changed(PartialMonthContribution policy) =>
        Assert.Equal(1m, PartialPeriodRules.Factor(policy, 1m));

    [Fact] public void Full_Period_Policy_Charges_The_Normal_Share_Even_When_Partial() =>
        Assert.Equal(1m, PartialPeriodRules.Factor(PartialMonthContribution.FullPeriod, 0.375m));

    [Fact] public void By_Days_Employed_Uses_The_Fraction() =>
        Assert.Equal(0.375m, PartialPeriodRules.Factor(PartialMonthContribution.ByDaysEmployed, 0.375m));

    [Fact] public void Skip_If_Partial_Charges_Nothing() =>
        Assert.Equal(0m, PartialPeriodRules.Factor(PartialMonthContribution.SkipIfPartial, 0.375m));

    [Fact] public void Apply_Rounds_To_Cents_Away_From_Zero() =>
        Assert.Equal(55.55m, PartialPeriodRules.Apply(100m, 0.5555m));   // 55.55 exactly

    [Fact] public void A_Factor_Of_One_Returns_The_Amount_Untouched() =>
        Assert.Equal(162.50m, PartialPeriodRules.Apply(162.50m, 1m));
}