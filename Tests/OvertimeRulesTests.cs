using Core.Common;

namespace Tests;

public class OvertimeRulesTests
{
    [Fact]
    public void Two_Hours_Is_120_Minutes()
    {
        var r = OvertimeRules.Measure(new TimeOnly(18, 0), new TimeOnly(20, 0));
        Assert.Null(r.Error);
        Assert.Equal(120, r.Minutes);
    }

    [Fact]
    public void End_Before_Start_Is_Rejected() =>
        Assert.NotNull(OvertimeRules.Measure(new TimeOnly(20, 0), new TimeOnly(18, 0)).Error);

    [Fact]
    public void Ten_Minutes_Is_Too_Short() =>
        Assert.NotNull(OvertimeRules.Measure(new TimeOnly(18, 0), new TimeOnly(18, 10)).Error);

    [Fact]
    public void Thirteen_Hours_Is_Too_Long() =>
        Assert.NotNull(OvertimeRules.Measure(new TimeOnly(6, 0), new TimeOnly(19, 0)).Error);

    [Fact]
    public void Overlapping_Ranges_Are_Detected() =>
        Assert.True(OvertimeRules.Overlaps(new TimeOnly(18, 0), new TimeOnly(20, 0), new TimeOnly(19, 0), new TimeOnly(21, 0)));

    [Fact]
    public void Back_To_Back_Ranges_Do_Not_Overlap() =>
        Assert.False(OvertimeRules.Overlaps(new TimeOnly(18, 0), new TimeOnly(20, 0), new TimeOnly(20, 0), new TimeOnly(22, 0)));
}