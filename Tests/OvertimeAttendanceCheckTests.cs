using Core.Common;

namespace Tests;

public class OvertimeAttendanceCheckTests
{
    private static readonly DateOnly Day = new(2026, 11, 3);
    private static readonly TimeOnly End = new(20, 0);

    [Fact] public void A_Clock_Out_After_The_Overtime_End_Supports_It() =>
        Assert.Null(OvertimeAttendanceCheck.Check(Day, End, true, new TimeOnly(20, 5)));

    [Fact] public void A_Clock_Out_Exactly_At_The_End_Supports_It() =>
        Assert.Null(OvertimeAttendanceCheck.Check(Day, End, true, End));

    [Fact] public void No_Record_Is_Flagged() =>
        Assert.Contains("no attendance record", OvertimeAttendanceCheck.Check(Day, End, false, null));

    [Fact] public void An_Open_Record_Is_Flagged() =>
        Assert.Contains("no clock-out", OvertimeAttendanceCheck.Check(Day, End, true, null));

    [Fact] public void Leaving_Before_The_Overtime_Ended_Is_Flagged_With_Both_Times()
    {
        var msg = OvertimeAttendanceCheck.Check(Day, End, true, new TimeOnly(18, 30));
        Assert.Contains("20:00", msg);
        Assert.Contains("18:30", msg);
    }
}