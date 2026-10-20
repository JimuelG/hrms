using Core.Common;

namespace Tests;

public class SalarySegmenterTests
{
    private static DateOnly D(int m, int d) => new(2026, m, d);
    private static readonly DateOnly Old = new(2020, 1, 1);

    [Fact] public void No_Change_Is_One_Segment_Over_The_Whole_Window()
    {
        var s = SalarySegmenter.Split([new(Old, 20000m)], D(10, 1), D(10, 15));
        Assert.Single(s);
        Assert.Equal(15, s[0].Days);
    }

    [Fact] public void A_Mid_Window_Raise_Splits_Into_Two_Contiguous_Segments()
    {
        var s = SalarySegmenter.Split([new(Old, 20000m), new(D(10, 10), 30000m)], D(10, 1), D(10, 15));

        Assert.Equal(2, s.Count);
        Assert.Equal((D(10, 1), D(10, 9), 20000m), (s[0].Start, s[0].End, s[0].Monthly));
        Assert.Equal((D(10, 10), D(10, 15), 30000m), (s[1].Start, s[1].End, s[1].Monthly));
        Assert.Equal(15, s.Sum(x => x.Days));
    }

    [Fact] public void A_Raise_On_The_First_Day_Is_The_Opening_Rate_Not_A_Split()
    {
        var s = SalarySegmenter.Split([new(Old, 20000m), new(D(10, 1), 30000m)], D(10, 1), D(10, 15));
        Assert.Single(s);
        Assert.Equal(30000m, s[0].Monthly);
    }

    [Fact] public void A_Raise_After_The_Window_Is_Ignored()
    {
        var s = SalarySegmenter.Split([new(Old, 20000m), new(D(10, 20), 30000m)], D(10, 1), D(10, 15));
        Assert.Single(s);
        Assert.Equal(20000m, s[0].Monthly);
    }

    [Fact] public void Two_Raises_Make_Three_Segments_That_Cover_Every_Day_Once()
    {
        var s = SalarySegmenter.Split(
            [new(Old, 20000m), new(D(10, 6), 25000m), new(D(10, 11), 30000m)], D(10, 1), D(10, 15));

        Assert.Equal(3, s.Count);
        Assert.Equal(15, s.Sum(x => x.Days));
        for (var i = 1; i < s.Count; i++) Assert.Equal(s[i - 1].End.AddDays(1), s[i].Start);
    }

    [Fact] public void History_Order_Does_Not_Matter() =>
        Assert.Equal(2, SalarySegmenter.Split([new(D(10, 10), 30000m), new(Old, 20000m)], D(10, 1), D(10, 15)).Count);

    [Fact] public void No_Salary_On_The_First_Day_Gives_Nothing()
    {
        Assert.Empty(SalarySegmenter.Split([new(D(10, 5), 20000m)], D(10, 1), D(10, 15)));
        Assert.Empty(SalarySegmenter.Split([], D(10, 1), D(10, 15)));
    }

    [Fact] public void An_Inverted_Window_Gives_Nothing() =>
        Assert.Empty(SalarySegmenter.Split([new(Old, 20000m)], D(10, 15), D(10, 1)));

    // ---- employed window ----
    private static readonly PayPeriod Period = new(D(10, 1), D(10, 15));

    [Fact] public void A_Full_Period_Window_Is_The_Whole_Period() =>
        Assert.Equal((D(10, 1), D(10, 15)), PayPeriodCalculator.EmployedWindow(Period, Old, null));

    [Fact] public void Hire_And_Last_Day_Both_Trim_The_Window() =>
        Assert.Equal((D(10, 4), D(10, 12)), PayPeriodCalculator.EmployedWindow(Period, D(10, 4), D(10, 12)));

    [Fact] public void A_Last_Day_Before_The_Period_Or_A_Hire_After_It_Means_Not_Employed()
    {
        Assert.Null(PayPeriodCalculator.EmployedWindow(Period, Old, D(9, 30)));
        Assert.Null(PayPeriodCalculator.EmployedWindow(Period, D(10, 16), null));
    }

    [Fact] public void A_Last_Day_After_The_Period_Does_Not_Extend_It() =>
        Assert.Equal((D(10, 1), D(10, 15)), PayPeriodCalculator.EmployedWindow(Period, Old, D(11, 30)));
}