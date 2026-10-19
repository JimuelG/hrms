using Core.Common;

namespace Tests;

public class PayPeriodCalculatorTests
{
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    [Fact] public void First_Half_Of_A_Month() =>
        Assert.Equal(new PayPeriod(D(2026, 10, 1), D(2026, 10, 15)), PayPeriodCalculator.Containing(D(2026, 10, 9), PayFrequency.SemiMonthly));

    [Fact] public void The_Fifteenth_Belongs_To_The_First_Half() =>
        Assert.Equal(D(2026, 10, 15), PayPeriodCalculator.Containing(D(2026, 10, 15), PayFrequency.SemiMonthly).End);

    [Fact] public void The_Sixteenth_Starts_The_Second_Half() =>
        Assert.Equal(new PayPeriod(D(2026, 10, 16), D(2026, 10, 31)), PayPeriodCalculator.Containing(D(2026, 10, 16), PayFrequency.SemiMonthly));

    [Theory]
    [InlineData(2026, 2, 28)]
    [InlineData(2028, 2, 29)]
    [InlineData(2026, 4, 30)]
    public void The_Second_Half_Ends_On_The_Last_Day_Of_The_Month(int y, int m, int lastDay) =>
        Assert.Equal(D(y, m, lastDay), PayPeriodCalculator.Containing(D(y, m, 20), PayFrequency.SemiMonthly).End);

    [Fact] public void Monthly_Covers_The_Whole_Month() =>
        Assert.Equal(new PayPeriod(D(2026, 10, 1), D(2026, 10, 31)), PayPeriodCalculator.Containing(D(2026, 10, 20), PayFrequency.Monthly));

    [Fact] public void Periods_Chain_Without_Gaps_Across_A_Year_End()
    {
        var p = PayPeriodCalculator.Containing(D(2026, 12, 16), PayFrequency.SemiMonthly);
        var next = PayPeriodCalculator.Next(p, PayFrequency.SemiMonthly);
        Assert.Equal(new PayPeriod(D(2027, 1, 1), D(2027, 1, 15)), next);
    }

    [Fact] public void Between_Lists_Every_Overlapping_Period()
    {
        var list = PayPeriodCalculator.Between(D(2026, 10, 10), D(2026, 11, 3), PayFrequency.SemiMonthly);
        Assert.Equal(3, list.Count);
        Assert.Equal(D(2026, 10, 1), list[0].Start);
        Assert.Equal(D(2026, 11, 15), list[^1].End);
    }

    [Fact] public void An_Inverted_Range_Is_Empty() =>
        Assert.Empty(PayPeriodCalculator.Between(D(2026, 11, 1), D(2026, 10, 1), PayFrequency.Monthly));

    [Fact] public void A_Full_Period_Is_Fraction_One() =>
        Assert.Equal(1m, PayPeriodCalculator.EmployedFraction(new(D(2026, 10, 1), D(2026, 10, 15)), D(2020, 1, 1), null));

    [Fact] public void A_Mid_Period_Hire_Is_Prorated_By_Calendar_Days()
    {
        // hired Oct 8: Oct 8 to Oct 15 is 8 of 15 days
        var f = PayPeriodCalculator.EmployedFraction(new(D(2026, 10, 1), D(2026, 10, 15)), D(2026, 10, 8), null);
        Assert.Equal(decimal.Round(8m / 15m, 10), f);
    }

    [Fact] public void A_Last_Day_Mid_Period_Prorates_The_End()
    {
        var f = PayPeriodCalculator.EmployedFraction(new(D(2026, 10, 1), D(2026, 10, 15)), D(2020, 1, 1), D(2026, 10, 5));
        Assert.Equal(decimal.Round(5m / 15m, 10), f);
    }

    [Fact] public void Not_Employed_At_All_Is_Zero()
    {
        var p = new PayPeriod(D(2026, 10, 1), D(2026, 10, 15));
        Assert.Equal(0m, PayPeriodCalculator.EmployedFraction(p, D(2026, 10, 20), null));
        Assert.Equal(0m, PayPeriodCalculator.EmployedFraction(p, D(2020, 1, 1), D(2026, 9, 30)));
    }
}