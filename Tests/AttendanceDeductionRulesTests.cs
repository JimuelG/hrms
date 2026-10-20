using Core.Common;
using Core.Enums;

namespace Tests;

public class AttendanceDeductionRulesTests
{
    private static readonly TimeOnly Nine = new(9, 0), Six = new(18, 0);

    [Theory]
    [InlineData(9, 0, 0)]      // on time
    [InlineData(9, 10, 10)]
    [InlineData(8, 30, 0)]     // early is never negative
    public void Late_Minutes_Are_Counted_From_The_Scheduled_Start(int h, int m, int expected) =>
        Assert.Equal(expected, AttendanceDeductionRules.LateMinutes(Nine, new TimeOnly(h, m), 0));

    [Fact] public void The_Grace_Period_Is_Subtracted_Not_A_Free_Pass_Beyond_It()
    {
        Assert.Equal(0, AttendanceDeductionRules.LateMinutes(Nine, new TimeOnly(9, 10), 10));
        Assert.Equal(5, AttendanceDeductionRules.LateMinutes(Nine, new TimeOnly(9, 15), 10));
    }

    [Theory]
    [InlineData(18, 0, 0)]
    [InlineData(17, 30, 30)]
    [InlineData(19, 0, 0)]     // staying late is overtime's business
    public void Undertime_Is_Counted_Back_From_The_Scheduled_End(int h, int m, int expected) =>
        Assert.Equal(expected, AttendanceDeductionRules.UndertimeMinutes(Six, new TimeOnly(h, m), 0));

    private static readonly WorkingDay MonFri = WorkingDay.Monday | WorkingDay.Tuesday | WorkingDay.Wednesday
        | WorkingDay.Thursday | WorkingDay.Friday;
    private static DateOnly D(int d) => new(2026, 11, d);   // Nov 2, 2026 is a Monday
    private static readonly IReadOnlySet<DateOnly> None = new HashSet<DateOnly>();

    [Fact] public void A_Working_Day_With_No_Explanation_Is_An_Absence() =>
        Assert.Equal([D(2), D(3), D(4), D(5), D(6)], AttendanceDeductionRules.AbsentDates(D(2), D(8), MonFri, None, None, None));

    [Fact] public void Weekends_Are_Never_Absences() =>
        Assert.Empty(AttendanceDeductionRules.AbsentDates(D(7), D(8), MonFri, None, None, None));

    [Fact] public void Attendance_Leave_And_Holidays_Each_Excuse_A_Day()
    {
        var absent = AttendanceDeductionRules.AbsentDates(D(2), D(6), MonFri,
            new HashSet<DateOnly> { D(2) }, new HashSet<DateOnly> { D(3) }, new HashSet<DateOnly> { D(4) });
        Assert.Equal([D(5), D(6)], absent);
    }

    [Fact] public void The_Employees_Own_Schedule_Decides_Which_Days_Count()
    {
        var monSat = MonFri | WorkingDay.Saturday;
        Assert.Single(AttendanceDeductionRules.AbsentDates(D(7), D(8), monSat, None, None, None));   // Saturday only
    }

    [Fact] public void The_Cap_Never_Exceeds_The_Basic_Salary_Or_Goes_Negative()
    {
        Assert.Equal(100m, AttendanceDeductionRules.Capped(100m, 500m));
        Assert.Equal(500m, AttendanceDeductionRules.Capped(900m, 500m));
        Assert.Equal(0m, AttendanceDeductionRules.Capped(100m, -5m));
    }
}