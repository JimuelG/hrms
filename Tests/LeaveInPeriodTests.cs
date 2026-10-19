using Core.Common;
using Core.Enums;

namespace Tests;

public class LeaveInPeriodTests
{
    private static DateOnly D(int m, int d) => new(2026, m, d);
    private static LeaveDayLine Counted(DateOnly date, decimal v = 1m) => new(date, v, LeaveDayStatus.Counted, null);

    private static readonly PayPeriod FirstHalf = new(D(10, 1), D(10, 15));

    [Fact] public void Only_Days_Inside_The_Period_Count()
    {
        var lines = new[] { Counted(D(10, 14)), Counted(D(10, 15)), Counted(D(10, 16)), Counted(D(10, 19)) };
        Assert.Equal(2m, LeaveInPeriod.Days(lines, FirstHalf));
    }

    [Fact] public void A_Request_Spanning_Two_Periods_Splits_Without_Losing_Or_Doubling_A_Day()
    {
        var lines = new[] { Counted(D(10, 14)), Counted(D(10, 15)), Counted(D(10, 16)), Counted(D(10, 19)) };
        var second = new PayPeriod(D(10, 16), D(10, 31));
        Assert.Equal(lines.Sum(l => l.Value),
            LeaveInPeriod.Days(lines, FirstHalf) + LeaveInPeriod.Days(lines, second));
    }

    [Fact] public void Half_Days_Count_As_Half() =>
        Assert.Equal(1.5m, LeaveInPeriod.Days([Counted(D(10, 14), 0.5m), Counted(D(10, 15))], FirstHalf));

    [Fact] public void Holidays_And_Days_Off_Never_Count()
    {
        var lines = new[]
        {
            new LeaveDayLine(D(10, 5), 0m, LeaveDayStatus.Holiday, "Holiday"),
            new LeaveDayLine(D(10, 3), 0m, LeaveDayStatus.NonWorkingDay, null),
            Counted(D(10, 6))
        };
        Assert.Equal(1m, LeaveInPeriod.Days(lines, FirstHalf));
    }

    [Fact] public void The_First_And_Last_Day_Of_A_Period_Are_Included()
    {
        Assert.Equal(2m, LeaveInPeriod.Days([Counted(D(10, 1)), Counted(D(10, 15))], FirstHalf));
    }

    [Fact] public void No_Lines_Means_No_Days() => Assert.Equal(0m, LeaveInPeriod.Days([], FirstHalf));

    [Theory]
    [InlineData(WorkingDay.Monday | WorkingDay.Tuesday | WorkingDay.Wednesday | WorkingDay.Thursday | WorkingDay.Friday, 5)]
    [InlineData(WorkingDay.Monday | WorkingDay.Tuesday | WorkingDay.Wednesday | WorkingDay.Thursday | WorkingDay.Friday | WorkingDay.Saturday, 6)]
    [InlineData(WorkingDay.Monday | WorkingDay.Tuesday | WorkingDay.Wednesday, 5)]
    public void The_Work_Week_Is_Five_Or_Six_Days(WorkingDay days, int expected) =>
        Assert.Equal(expected, LeaveInPeriod.WorkingDaysPerWeek(days));
}