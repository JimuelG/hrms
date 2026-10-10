using Core.Common;

namespace Tests;

public class ClockPolicyTests
{
    private static GeofenceResult R(GeofenceOutcome o) => new(o, Guid.NewGuid(), "HQ", 340, 150);

    [Fact]
    public void ClockIn_Inside_Is_Clean()
    {
        var d = ClockPolicy.Decide(ClockEvent.In, R(GeofenceOutcome.Inside), 10);
        Assert.True(d.Accepted); Assert.False(d.Flagged);
    }

    [Fact]
    public void ClockIn_Borderline_Is_Accepted_And_Flagged()
    {
        var d = ClockPolicy.Decide(ClockEvent.In, R(GeofenceOutcome.Borderline), 40);
        Assert.True(d.Accepted); Assert.True(d.Flagged);
    }

    [Fact]
    public void ClockIn_Outside_Is_Rejected_With_Distance_And_Name()
    {
        var d = ClockPolicy.Decide(ClockEvent.In, R(GeofenceOutcome.Outside), 10);
        Assert.False(d.Accepted);
        Assert.Contains("340", d.RejectReason);
        Assert.Contains("HQ", d.RejectReason);
    }

    [Fact]
    public void ClockIn_With_Weak_Signal_Is_Rejected() =>
        Assert.False(ClockPolicy.Decide(ClockEvent.In, R(GeofenceOutcome.AccuracyTooLow), 450).Accepted);

    [Fact]
    public void ClockIn_Without_Coordinates_Is_Rejected() =>
        Assert.False(ClockPolicy.Decide(ClockEvent.In, null, null).Accepted);

    [Fact]
    public void ClockOut_Inside_Is_Clean()
    {
        var d = ClockPolicy.Decide(ClockEvent.Out, R(GeofenceOutcome.Inside), 10);
        Assert.True(d.Accepted); Assert.False(d.Flagged);
    }

    [Theory]
    [InlineData(GeofenceOutcome.Borderline)]
    [InlineData(GeofenceOutcome.Outside)]
    [InlineData(GeofenceOutcome.AccuracyTooLow)]
    public void ClockOut_Never_Rejects_It_Flags(GeofenceOutcome outcome)
    {
        var d = ClockPolicy.Decide(ClockEvent.Out, R(outcome), 40);
        Assert.True(d.Accepted); Assert.True(d.Flagged);
    }

    [Fact]
    public void ClockOut_Without_Coordinates_Is_Accepted_And_Flagged()
    {
        var d = ClockPolicy.Decide(ClockEvent.Out, null, null);
        Assert.True(d.Accepted); Assert.True(d.Flagged);
    }
}