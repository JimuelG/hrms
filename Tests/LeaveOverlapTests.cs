using Core.Common;
using Core.Enums;

namespace Tests;

public class LeaveOverlapTests
{
    private static DateOnly D(int day) => new(2026, 11, day);   // Nov 2, 2026 is a Monday
    private static DateOnly[] Days(params int[] days) => days.Select(D).ToArray();

    private static HashSet<(DateOnly Date, int Half)> Slots(
        int start, int end, DayPortion sp, DayPortion ep, params int[] counted) =>
        LeaveOverlap.Slots(D(start), D(end), sp, ep, Days(counted));

    [Fact]
    public void A_Full_Day_Occupies_Both_Halves() =>
        Assert.Equal(2, Slots(2, 2, DayPortion.Full, DayPortion.Full, 2).Count);

    [Fact]
    public void A_Morning_And_An_Afternoon_On_The_Same_Day_Do_Not_Conflict()
    {
        var morning = Slots(2, 2, DayPortion.Morning, DayPortion.Morning, 2);
        var afternoon = Slots(2, 2, DayPortion.Afternoon, DayPortion.Afternoon, 2);
        Assert.False(LeaveOverlap.Overlaps(morning, afternoon));
    }

    [Fact]
    public void A_Full_Day_Conflicts_With_A_Half_Day_Of_The_Same_Day()
    {
        var full = Slots(2, 2, DayPortion.Full, DayPortion.Full, 2);
        var morning = Slots(2, 2, DayPortion.Morning, DayPortion.Morning, 2);
        Assert.True(LeaveOverlap.Overlaps(full, morning));
    }

    [Fact]
    public void A_Range_Ending_In_The_Morning_And_One_Starting_In_The_Afternoon_Can_Share_A_Day()
    {
        var first = Slots(2, 4, DayPortion.Full, DayPortion.Morning, 2, 3, 4);       // Mon to Wed morning
        var second = Slots(4, 6, DayPortion.Afternoon, DayPortion.Full, 4, 5, 6);    // Wed afternoon to Fri
        Assert.False(LeaveOverlap.Overlaps(first, second));
    }

    [Fact]
    public void Ranges_That_Share_A_Full_Day_Conflict()
    {
        var first = Slots(2, 4, DayPortion.Full, DayPortion.Full, 2, 3, 4);
        var second = Slots(4, 6, DayPortion.Full, DayPortion.Full, 4, 5, 6);
        Assert.True(LeaveOverlap.Overlaps(first, second));
    }

    [Fact]
    public void Only_Counted_Days_Occupy_Slots()
    {
        // Fri Nov 6 to Mon Nov 9: the weekend isn't counted, so it can't conflict with anything
        var friToMon = Slots(6, 9, DayPortion.Full, DayPortion.Full, 6, 9);
        var weekend = Slots(7, 8, DayPortion.Full, DayPortion.Full);
        Assert.False(LeaveOverlap.Overlaps(friToMon, weekend));
        Assert.Equal(4, friToMon.Count);
    }

    [Fact]
    public void Counted_Dates_Outside_The_Range_Are_Ignored() =>
        Assert.Empty(Slots(2, 2, DayPortion.Full, DayPortion.Full, 9));

    [Fact]
    public void Separate_Weeks_Do_Not_Conflict()
    {
        var a = Slots(2, 6, DayPortion.Full, DayPortion.Full, 2, 3, 4, 5, 6);
        var b = Slots(9, 13, DayPortion.Full, DayPortion.Full, 9, 10, 11, 12, 13);
        Assert.False(LeaveOverlap.Overlaps(a, b));
    }
}