using Core.Common;

namespace Tests;

public class GeofenceEvaluatorTests
{
    private const double BaseLat = 15.4755, BaseLon = 120.5963;
    private static double North(double meters) => BaseLat + meters / 111_194.9;

    private static GeofenceCandidate Fence(string name, double metersNorth, int radius) =>
        new(Guid.NewGuid(), name, North(metersNorth), BaseLon, radius);

    private static GeofenceResult At(double metersNorth, double accuracy, params GeofenceCandidate[] fences) =>
        GeofenceEvaluator.Evaluate(North(metersNorth), BaseLon, accuracy, fences);

    [Fact]
    public void Circle_Fully_Inside_The_Fence_Is_Inside()
    {
        var r = At(50, 10, Fence("HQ", 0, 150));
        Assert.Equal(GeofenceOutcome.Inside, r.Outcome);
        Assert.InRange(r.DistanceMeters!.Value, 49, 51);
        Assert.Equal("HQ", r.LocationName);
    }

    [Fact]
    public void Circle_Straddling_The_Edge_Is_Borderline() =>
        Assert.Equal(GeofenceOutcome.Borderline, At(140, 30, Fence("HQ", 0, 150)).Outcome);

    [Fact]
    public void Circle_Fully_Outside_Is_Outside() =>
        Assert.Equal(GeofenceOutcome.Outside, At(400, 20, Fence("HQ", 0, 150)).Outcome);

    [Fact]
    public void Accuracy_Above_The_Cap_Is_Unusable_Even_At_The_Center() =>
        Assert.Equal(GeofenceOutcome.AccuracyTooLow, At(0, 200.1, Fence("HQ", 0, 150)).Outcome);

    [Fact]
    public void Accuracy_Exactly_At_The_Cap_Is_Still_Evaluated() =>
        Assert.Equal(GeofenceOutcome.Borderline, At(0, 200, Fence("HQ", 0, 150)).Outcome);

    [Fact]
    public void No_Candidates_Reports_NoCandidates() =>
        Assert.Equal(GeofenceOutcome.NoCandidates, At(0, 10).Outcome);

    [Fact]
    public void Picks_The_Fence_The_Employee_Is_Actually_Inside()
    {
        var r = At(280, 10, Fence("A", 0, 150), Fence("B", 300, 100));
        Assert.Equal(GeofenceOutcome.Inside, r.Outcome);
        Assert.Equal("B", r.LocationName);
    }

    [Fact]
    public void Prefers_Inside_Over_A_Nearer_Borderline()
    {
        // A is nearer but only borderline; B is farther but comfortably inside.
        var r = At(0, 30, Fence("A", 40, 50), Fence("B", -100, 500));
        Assert.Equal(GeofenceOutcome.Inside, r.Outcome);
        Assert.Equal("B", r.LocationName);
    }

    [Fact]
    public void Outside_Reports_The_Nearest_Fence()
    {
        var r = At(0, 10, Fence("Far", 1000, 100), Fence("Near", 500, 100));
        Assert.Equal(GeofenceOutcome.Outside, r.Outcome);
        Assert.Equal("Near", r.LocationName);
        Assert.InRange(r.DistanceMeters!.Value, 495, 505);
    }
}