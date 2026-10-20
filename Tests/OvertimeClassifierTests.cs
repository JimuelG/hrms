using Core.Common;
using Core.Enums;

namespace Tests;

public class OvertimeClassifierTests
{
    private static readonly WorkingDay MonFri = WorkingDay.Monday | WorkingDay.Tuesday | WorkingDay.Wednesday
        | WorkingDay.Thursday | WorkingDay.Friday;
    private static DateOnly D(int d) => new(2026, 11, d);    // Nov 2, 2026 is a Monday
    private static readonly IReadOnlySet<DateOnly> NoHolidays = new HashSet<DateOnly>();

    [Fact] public void A_Working_Day_Is_Ordinary() =>
        Assert.Equal(OvertimeDayType.Ordinary, OvertimeClassifier.Classify(D(3), MonFri, NoHolidays));

    [Fact] public void A_Day_Off_In_The_Schedule_Is_A_Rest_Day() =>
        Assert.Equal(OvertimeDayType.RestDay, OvertimeClassifier.Classify(D(7), MonFri, NoHolidays));

    [Fact] public void The_Employees_Own_Schedule_Decides_What_Is_A_Rest_Day()
    {
        var monSat = MonFri | WorkingDay.Saturday;
        Assert.Equal(OvertimeDayType.Ordinary, OvertimeClassifier.Classify(D(7), monSat, NoHolidays));
        Assert.Equal(OvertimeDayType.RestDay, OvertimeClassifier.Classify(D(8), monSat, NoHolidays));
    }

    [Fact] public void A_Holiday_Is_A_Holiday() =>
        Assert.Equal(OvertimeDayType.Holiday, OvertimeClassifier.Classify(D(4), MonFri, new HashSet<DateOnly> { D(4) }));

    [Fact] public void A_Holiday_On_A_Rest_Day_Is_A_Holiday() =>
        Assert.Equal(OvertimeDayType.Holiday, OvertimeClassifier.Classify(D(7), MonFri, new HashSet<DateOnly> { D(7) }));

    [Fact] public void Bucketing_Sums_By_Type_And_Drops_Empty()
    {
        var b = OvertimeClassifier.Bucket([(OvertimeDayType.Holiday, 60), (OvertimeDayType.Ordinary, 30),
            (OvertimeDayType.Ordinary, 90), (OvertimeDayType.RestDay, 0)]);

        Assert.Equal(2, b.Count);
        Assert.Equal(120, b.Single(x => x.DayType == OvertimeDayType.Ordinary).Minutes);
        Assert.Equal(60, b.Single(x => x.DayType == OvertimeDayType.Holiday).Minutes);
    }

    [Fact] public void Ordinary_Uses_Its_Multiplier_And_Never_Falls_Back() =>
        Assert.Equal((1.25m, false), OvertimeClassifier.MultiplierFor(OvertimeDayType.Ordinary, new PayrollSettings()));

    [Fact] public void An_Unset_Premium_Falls_Back_To_Ordinary_And_Says_So()
    {
        var s = new PayrollSettings();
        Assert.Equal((1.25m, true), OvertimeClassifier.MultiplierFor(OvertimeDayType.RestDay, s));
        Assert.Equal((1.25m, true), OvertimeClassifier.MultiplierFor(OvertimeDayType.Holiday, s));
    }

    [Fact] public void A_Set_Premium_Is_Used()
    {
        var s = new PayrollSettings(RestDayOvertimeMultiplier: 1.3m, HolidayOvertimeMultiplier: 2m);
        Assert.Equal((1.3m, false), OvertimeClassifier.MultiplierFor(OvertimeDayType.RestDay, s));
        Assert.Equal((2m, false), OvertimeClassifier.MultiplierFor(OvertimeDayType.Holiday, s));
    }
}