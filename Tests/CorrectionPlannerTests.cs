using Core.Common;

namespace Tests;

public class CorrectionPlannerTests
{
    private static readonly DateOnly Day = new(2026, 10, 10);
    private static readonly DateTime Now = new(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc);
    private const string Manila = "Asia/Manila";   // UTC+8, no daylight saving

    private static DateTime Utc(int d, int h, int m = 0) => new(2026, 10, d, h, m, 0, DateTimeKind.Utc);

    [Fact]
    public void Neither_Time_Is_Rejected() =>
        Assert.NotNull(CorrectionPlanner.Plan(Day, Manila, null, null, null, null, Now).Error);

    [Fact]
    public void A_New_Record_Needs_Both_Times() =>
        Assert.NotNull(CorrectionPlanner.Plan(Day, Manila, null, null, new TimeOnly(9, 0), null, Now).Error);

    [Fact]
    public void A_New_Record_Converts_Local_Times_To_Utc()
    {
        var plan = CorrectionPlanner.Plan(Day, Manila, null, null, new TimeOnly(9, 0), new TimeOnly(18, 0), Now);
        Assert.Null(plan.Error);
        Assert.Equal(Utc(10, 1), plan.ClockInUtc);     // 09:00 Manila
        Assert.Equal(Utc(10, 10), plan.ClockOutUtc);   // 18:00 Manila
    }

    [Fact]
    public void Adding_A_ClockOut_To_An_Open_Record_Keeps_The_ClockIn()
    {
        var plan = CorrectionPlanner.Plan(Day, Manila, Utc(10, 1), null, null, new TimeOnly(18, 0), Now);
        Assert.Null(plan.Error);
        Assert.Equal(Utc(10, 1), plan.ClockInUtc);
        Assert.Equal(Utc(10, 10), plan.ClockOutUtc);
    }

    [Fact]
    public void Changing_Only_The_ClockIn_Keeps_The_Existing_ClockOut()
    {
        var plan = CorrectionPlanner.Plan(Day, Manila, Utc(10, 2), Utc(10, 10), new TimeOnly(8, 30), null, Now);
        Assert.Null(plan.Error);
        Assert.Equal(Utc(10, 0, 30), plan.ClockInUtc);
        Assert.Equal(Utc(10, 10), plan.ClockOutUtc);
    }

    [Fact]
    public void ClockOut_Not_After_ClockIn_Is_Rejected() =>
        Assert.NotNull(CorrectionPlanner.Plan(Day, Manila, null, null, new TimeOnly(18, 0), new TimeOnly(9, 0), Now).Error);

    [Fact]
    public void A_Time_In_The_Future_Is_Rejected()
    {
        var now = Utc(10, 12);   // 20:00 in Manila; 23:00 Manila is still ahead
        var plan = CorrectionPlanner.Plan(Day, Manila, Utc(10, 1), null, null, new TimeOnly(23, 0), now);
        Assert.Contains("future", plan.Error);
    }

    [Fact]
    public void A_Time_Skipped_By_A_Clock_Change_Is_Rejected()
    {
        // 2026-03-08 02:30 doesn't exist in New York (clocks jump 02:00 -> 03:00)
        var plan = CorrectionPlanner.Plan(
            new DateOnly(2026, 3, 8), "America/New_York", null, null, new TimeOnly(2, 30), new TimeOnly(12, 0), Now);
        Assert.Contains("clock change", plan.Error);
    }

    [Fact]
    public void ToUtc_Treats_An_Unknown_Zone_As_Utc() =>
        Assert.Equal(Utc(10, 9), LocalClock.ToUtc(Day, new TimeOnly(9, 0), "Not/AZone"));
}