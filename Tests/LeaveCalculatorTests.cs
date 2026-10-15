using Core.Common;
using Core.Entities;
using Core.Enums;

namespace Tests;

public class LeaveCalculatorTests
{
    private static readonly WorkingDay MonFri = WorkingDay.Monday | WorkingDay.Tuesday | WorkingDay.Wednesday
        | WorkingDay.Thursday | WorkingDay.Friday;
    private static readonly IReadOnlyDictionary<DateOnly, string> NoHolidays = new Dictionary<DateOnly, string>();

    private static DateOnly D(int month, int day) => new(2026, month, day);

    private static LeaveCalculation Calc(
        DateOnly start, DateOnly end, DayPortion sp = DayPortion.Full, DayPortion ep = DayPortion.Full,
        WorkingDay? days = null, IReadOnlyDictionary<DateOnly, string>? holidays = null, bool half = true) =>
        LeaveCalculator.Calculate(start, end, sp, ep, days ?? MonFri, holidays ?? NoHolidays, half);

    [Fact]
    public void A_Single_Working_Day_Is_One_Day()
    {
        var r = Calc(D(10, 12), D(10, 12));
        Assert.Null(r.Error);
        Assert.Equal(1m, r.Days);
    }

    [Fact]
    public void A_Full_Working_Week_Is_Five_Days() =>
        Assert.Equal(5m, Calc(D(10, 12), D(10, 16)).Days);

    [Fact]
    public void Weekends_Are_Skipped_And_Reported()
    {
        var r = Calc(D(10, 16), D(10, 19));   // Friday to Monday
        Assert.Equal(2m, r.Days);
        Assert.Equal(LeaveDayStatus.NonWorkingDay, r.Lines.Single(l => l.Date == D(10, 17)).Status);
    }

    [Fact]
    public void A_Holiday_Is_Skipped_And_Named()
    {
        var holidays = new Dictionary<DateOnly, string> { [D(10, 14)] = "Founders Day" };
        var r = Calc(D(10, 12), D(10, 16), holidays: holidays);
        Assert.Equal(4m, r.Days);

        var line = r.Lines.Single(l => l.Date == D(10, 14));
        Assert.Equal(LeaveDayStatus.Holiday, line.Status);
        Assert.Equal("Founders Day", line.HolidayName);
    }

    [Fact]
    public void A_Holiday_On_A_Day_Off_Is_Reported_As_A_Day_Off()
    {
        var holidays = new Dictionary<DateOnly, string> { [D(10, 17)] = "Saturday holiday" };
        var r = Calc(D(10, 16), D(10, 19), holidays: holidays);
        Assert.Equal(2m, r.Days);
        Assert.Equal(LeaveDayStatus.NonWorkingDay, r.Lines.Single(l => l.Date == D(10, 17)).Status);
    }

    [Fact]
    public void A_Single_Half_Day_Is_Half_A_Day() =>
        Assert.Equal(0.5m, Calc(D(10, 12), D(10, 12), DayPortion.Morning, DayPortion.Morning).Days);

    [Fact]
    public void Afternoon_Start_And_Morning_End_Are_Each_Half_A_Day() =>
        Assert.Equal(2m, Calc(D(10, 12), D(10, 14), DayPortion.Afternoon, DayPortion.Morning).Days);   // 0.5 + 1 + 0.5

    [Fact]
    public void The_Employees_Own_Schedule_Decides_Which_Days_Count()
    {
        var tueToSat = WorkingDay.Tuesday | WorkingDay.Wednesday | WorkingDay.Thursday | WorkingDay.Friday | WorkingDay.Saturday;
        Assert.Equal(2m, Calc(D(10, 16), D(10, 18), days: tueToSat).Days);   // Fri and Sat count, Sun doesn't
    }

    [Fact]
    public void Half_Days_Are_Rejected_When_The_Type_Does_Not_Allow_Them()
    {
        var r = Calc(D(10, 12), D(10, 12), DayPortion.Morning, DayPortion.Morning, half: false);
        Assert.Contains("half-days", r.Error);
        Assert.Null(Calc(D(10, 12), D(10, 12), half: false).Error);
    }

    [Fact]
    public void A_Multi_Day_Range_Cannot_Start_With_Only_The_Morning() =>
        Assert.Contains("afternoon only", Calc(D(10, 12), D(10, 14), DayPortion.Morning).Error);

    [Fact]
    public void A_Multi_Day_Range_Cannot_End_With_Only_The_Afternoon() =>
        Assert.Contains("morning only", Calc(D(10, 12), D(10, 14), ep: DayPortion.Afternoon).Error);

    [Fact]
    public void A_Single_Day_Needs_Matching_Portions() =>
        Assert.Contains("must match", Calc(D(10, 12), D(10, 12), DayPortion.Morning, DayPortion.Afternoon).Error);

    [Fact]
    public void A_Half_Day_On_A_Day_Off_Is_Rejected() =>
        Assert.Contains("not a working day", Calc(D(10, 17), D(10, 17), DayPortion.Morning, DayPortion.Morning).Error);

    [Fact]
    public void End_Before_Start_Is_Rejected() =>
        Assert.NotNull(Calc(D(10, 14), D(10, 12)).Error);

    [Fact]
    public void A_Range_Of_Only_Days_Off_Is_Rejected() =>
        Assert.Contains("days off", Calc(D(10, 17), D(10, 18)).Error);

    [Fact]
    public void A_Schedule_With_No_Working_Days_Is_Rejected() =>
        Assert.Contains("No working days", Calc(D(10, 12), D(10, 12), days: WorkingDay.None).Error);

    [Fact]
    public void A_Full_Year_Is_The_Longest_Allowed_Range()
    {
        Assert.Null(Calc(new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1)).Error);                 // 366 days
        Assert.Contains("366", Calc(new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 2)).Error);      // 367 days
    }

    [Fact]
    public void Dates_Outside_The_Supported_Years_Are_Rejected() =>
        Assert.NotNull(Calc(new DateOnly(2101, 1, 1), new DateOnly(2101, 1, 2)).Error);

    [Theory]
    [InlineData(3, 2, true)]
    [InlineData(2, 2, false)]      // exactly at the threshold: not required yet
    [InlineData(2.5, 2, true)]
    [InlineData(5, null, false)]   // no threshold, never required
    [InlineData(0.5, 0, true)]     // 0 means any leave needs one
    public void Document_Is_Required_Once_Past_The_Threshold(double days, int? after, bool expected) =>
        Assert.Equal(expected, LeaveCalculator.RequiresDocument((decimal)days, after));

    [Theory]
    [InlineData(DayOfWeek.Monday, WorkingDay.Monday)]
    [InlineData(DayOfWeek.Tuesday, WorkingDay.Tuesday)]
    [InlineData(DayOfWeek.Wednesday, WorkingDay.Wednesday)]
    [InlineData(DayOfWeek.Thursday, WorkingDay.Thursday)]
    [InlineData(DayOfWeek.Friday, WorkingDay.Friday)]
    [InlineData(DayOfWeek.Saturday, WorkingDay.Saturday)]
    [InlineData(DayOfWeek.Sunday, WorkingDay.Sunday)]
    public void Each_Day_Of_The_Week_Maps_To_Its_Flag(DayOfWeek day, WorkingDay expected) =>
        Assert.Equal(expected, day.ToWorkingDay());
}