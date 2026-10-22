using Core.Common;
using Core.Entities;
using Core.Enums;

namespace Tests;

public class ReportRulesTests
{
    private static DateOnly D(int d) => new(2026, 11, d);     // Nov 2, 2026 is a Monday
    private static readonly WorkingDay MonFri = WorkingDay.Monday | WorkingDay.Tuesday | WorkingDay.Wednesday
        | WorkingDay.Thursday | WorkingDay.Friday;
    private static readonly TimeOnly Nine = new(9, 0), Six = new(18, 0);

    // ---- range ----
    [Fact] public void A_Normal_Range_Is_Accepted() => Assert.Null(ReportRangeRules.Check(D(1), D(30)));

    [Fact] public void An_Inverted_Range_Is_Refused() =>
        Assert.Contains("before the start", ReportRangeRules.Check(D(10), D(1)));

    [Fact] public void Ninety_Three_Days_Is_The_Limit()
    {
        var from = new DateOnly(2026, 1, 1);
        Assert.Null(ReportRangeRules.Check(from, from.AddDays(92)));
        Assert.Contains("at most 93", ReportRangeRules.Check(from, from.AddDays(93)));
    }

    [Fact] public void Years_Out_Of_Range_Are_Refused() =>
        Assert.Contains("out of range", ReportRangeRules.Check(new DateOnly(1999, 1, 1), new DateOnly(1999, 1, 5)));

    // ---- attendance summary ----
    private static AttendanceDayFact Day(int d, TimeOnly cin, TimeOnly? cout = null, bool scheduled = true) =>
        new(D(d), cin, cout, scheduled ? Nine : null, scheduled ? Six : null);

    private static IReadOnlySet<DateOnly> Set(params int[] days) => days.Select(D).ToHashSet();

    private static AttendanceSummary Run(
        IReadOnlyList<AttendanceDayFact> days, int to = 6, int grace = 0, int[]? leave = null, int[]? holidays = null,
        int today = 20, bool hasSchedule = true, int overtime = 0, bool noWindow = false)
    {
        (DateOnly Start, DateOnly End)? window = noWindow ? null : (D(2), D(to));
        return AttendanceSummaryRules.Summarize(new(window, MonFri, hasSchedule, days,
            Set(leave ?? []), Set(holidays ?? []), overtime, grace, D(today)));
    }

    [Fact] public void A_Mixed_Week_Is_Counted_Day_By_Day()
    {
        var s = Run([
            Day(2, Nine, Six),                          // on time
            Day(3, new TimeOnly(9, 30), Six),           // 30 late
            Day(4, Nine, new TimeOnly(17, 0))           // 60 undertime
        ], leave: [5]);                                  // Thursday on leave, Friday absent

        Assert.Equal(5, s.ScheduledDays);
        Assert.Equal(3, s.PresentDays);
        Assert.Equal(1, s.AbsentDays);
        Assert.Equal((1, 30, 60, 0), (s.LateCount, s.LateMinutes, s.UndertimeMinutes, s.OpenRecords));
    }

    [Fact] public void Nobody_Showing_Up_Is_Five_Absences_In_A_Working_Week()
    {
        var s = Run([]);
        Assert.Equal((5, 0, 5), (s.ScheduledDays, s.PresentDays, s.AbsentDays));
    }

    [Fact] public void The_Grace_Period_Is_Subtracted_From_Each_Late()
    {
        var days = new[] { Day(3, new TimeOnly(9, 30), Six) };
        Assert.Equal((1, 20), (Run(days, grace: 10).LateCount, Run(days, grace: 10).LateMinutes));
        Assert.Equal((0, 0), (Run(days, grace: 30).LateCount, Run(days, grace: 30).LateMinutes));
    }

    [Fact] public void A_Holiday_Is_Not_Scheduled_And_Not_An_Absence()
    {
        var s = Run([Day(2, Nine, Six), Day(3, Nine, Six), Day(5, Nine, Six), Day(6, Nine, Six)], to: 8, holidays: [4]);
        Assert.Equal((4, 4, 0), (s.ScheduledDays, s.PresentDays, s.AbsentDays));
    }

    [Fact] public void Work_On_A_Day_Off_Counts_As_Present_But_Not_Scheduled()
    {
        var s = Run([Day(2, Nine, Six), Day(7, Nine, Six)], to: 8);   // Nov 7 is a Saturday
        Assert.Equal((5, 2, 4), (s.ScheduledDays, s.PresentDays, s.AbsentDays));
    }

    [Fact] public void A_Record_Without_A_Clock_Out_Is_Open_Only_Before_Today()
    {
        var days = new[] { Day(2, Nine) };
        Assert.Equal(1, Run(days, today: 20).OpenRecords);
        Assert.Equal(0, Run(days, today: 2).OpenRecords);
        Assert.Equal(0, Run(days, today: 20).UndertimeMinutes);   // no clock-out, no undertime guess
    }

    [Fact] public void Records_Outside_The_Window_Are_Ignored() =>
        Assert.Equal(0, Run([Day(9, Nine, Six)], to: 6).PresentDays);

    [Fact] public void A_Record_Without_A_Stored_Schedule_Is_Never_Late() =>
        Assert.Equal(0, Run([Day(3, new TimeOnly(9, 30), Six, scheduled: false)]).LateCount);

    [Fact] public void No_Schedule_Is_Flagged() => Assert.True(Run([], hasSchedule: false).NoSchedule);
    [Fact] public void A_Schedule_Is_Not_Flagged() => Assert.False(Run([]).NoSchedule);

    [Fact] public void Overtime_Minutes_Pass_Through() => Assert.Equal(90, Run([], overtime: 90).OvertimeMinutes);

    [Fact] public void Someone_Not_Employed_In_The_Window_Gets_Zeros()
    {
        var s = Run([], noWindow: true);
        Assert.Equal((0, 0, 0), (s.ScheduledDays, s.PresentDays, s.AbsentDays));
    }

    // ---- leave usage ----
    private static readonly Guid A = Guid.NewGuid(), B = Guid.NewGuid(), Vl = Guid.NewGuid(), Sl = Guid.NewGuid();

    private static readonly LeaveRequestFact[] Facts =
    [
        new(A, Vl, ApprovalStatus.Approved, 3m), new(A, Vl, ApprovalStatus.Approved, 2m),
        new(A, Vl, ApprovalStatus.Pending, 1m), new(A, Vl, ApprovalStatus.Rejected, 5m),
        new(A, Sl, ApprovalStatus.Approved, 1m), new(B, Vl, ApprovalStatus.Approved, 4m),
        new(B, Sl, ApprovalStatus.Cancelled, 2m)
    ];

    [Fact] public void Usage_Sums_Approved_And_Pending_Per_Person_And_Type()
    {
        var u = LeaveUsageRules.Usage(Facts).ToDictionary(x => (x.EmployeeId, x.LeaveTypeId));
        Assert.Equal((5m, 1m), (u[(A, Vl)].Approved, u[(A, Vl)].Pending));
        Assert.Equal((1m, 0m), (u[(A, Sl)].Approved, u[(A, Sl)].Pending));
        Assert.Equal((4m, 0m), (u[(B, Vl)].Approved, u[(B, Vl)].Pending));
    }

    [Fact] public void Rejected_And_Cancelled_Requests_Count_For_Nothing()
    {
        var u = LeaveUsageRules.Usage(Facts);
        Assert.Equal(3, u.Count);                                // B's only Sl request was cancelled
        Assert.DoesNotContain(u, x => x.EmployeeId == B && x.LeaveTypeId == Sl);
    }

    [Fact] public void Totals_Add_Up_Per_Leave_Type()
    {
        var t = LeaveUsageRules.TypeTotals(LeaveUsageRules.Usage(Facts)).ToDictionary(x => x.LeaveTypeId);
        Assert.Equal((9m, 1m), (t[Vl].Approved, t[Vl].Pending));
        Assert.Equal((1m, 0m), (t[Sl].Approved, t[Sl].Pending));
    }

    [Fact] public void No_Requests_Means_No_Rows() => Assert.Empty(LeaveUsageRules.Usage([]));

    // ---- leave calendar ----
    [Fact] public void A_Multi_Day_Leave_Appears_On_Each_Day_Of_The_Window()
    {
        var days = LeaveCalendarRules.Days([new("Ana", D(3), D(5))], D(2), D(8));
        Assert.Equal([D(3), D(4), D(5)], days.Select(d => d.Date));
    }

    [Fact] public void Leave_Is_Clipped_To_The_Window()
    {
        var days = LeaveCalendarRules.Days([new("Ana", D(3), D(10))], D(4), D(5));
        Assert.Equal([D(4), D(5)], days.Select(d => d.Date));
    }

    [Fact] public void Names_On_One_Day_Are_Alphabetical()
    {
        var days = LeaveCalendarRules.Days([new("Zed", D(4), D(4)), new("Ana", D(4), D(4))], D(2), D(8));
        Assert.Equal(["Ana", "Zed"], days.Single().Names);
    }

    [Fact] public void Days_Nobody_Is_Away_Are_Left_Out() =>
        Assert.Empty(LeaveCalendarRules.Days([new("Ana", D(20), D(21))], D(2), D(8)));

    [Fact] public void A_Duplicate_Name_On_One_Day_Appears_Once()
    {
        var days = LeaveCalendarRules.Days([new("Ana", D(4), D(4)), new("Ana", D(4), D(5))], D(4), D(4));
        Assert.Single(days.Single().Names);
    }
}