using Core.Common;

namespace Tests;

public class ContributionSpreaderTests
{
    [Fact] public void Monthly_Pay_Takes_The_Whole_Amount() =>
        Assert.Equal(1000m, ContributionSpreader.ForPeriod(1000m, PayFrequency.Monthly, true, ContributionDeduction.SplitEvenly));

    [Fact] public void Split_Evenly_Takes_Half_In_Each_Period()
    {
        Assert.Equal(500m, ContributionSpreader.ForPeriod(1000m, PayFrequency.SemiMonthly, true, ContributionDeduction.SplitEvenly));
        Assert.Equal(500m, ContributionSpreader.ForPeriod(1000m, PayFrequency.SemiMonthly, false, ContributionDeduction.SplitEvenly));
    }

    [Fact] public void An_Odd_Cent_Goes_To_The_First_Period()
    {
        Assert.Equal(50.01m, ContributionSpreader.ForPeriod(100.01m, PayFrequency.SemiMonthly, true, ContributionDeduction.SplitEvenly));
        Assert.Equal(50.00m, ContributionSpreader.ForPeriod(100.01m, PayFrequency.SemiMonthly, false, ContributionDeduction.SplitEvenly));
    }

    [Fact] public void Second_Period_Only_Takes_Nothing_In_The_First()
    {
        Assert.Equal(0m, ContributionSpreader.ForPeriod(1000m, PayFrequency.SemiMonthly, true, ContributionDeduction.SecondPeriodOnly));
        Assert.Equal(1000m, ContributionSpreader.ForPeriod(1000m, PayFrequency.SemiMonthly, false, ContributionDeduction.SecondPeriodOnly));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(99.99)]
    [InlineData(1234.57)]
    public void The_Two_Halves_Always_Add_Up_To_The_Month(double amount)
    {
        var m = (decimal)amount;
        var first = ContributionSpreader.ForPeriod(m, PayFrequency.SemiMonthly, true, ContributionDeduction.SplitEvenly);
        var second = ContributionSpreader.ForPeriod(m, PayFrequency.SemiMonthly, false, ContributionDeduction.SplitEvenly);
        Assert.Equal(m, first + second);
    }
}