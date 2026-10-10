namespace Core.Common;

public enum GeofenceOutcome
{
    Inside,
    Borderline,
    Outside,
    AccuracyTooLow,
    NoCandidates
}

public sealed record GeofenceCandidate(Guid LocationId, string Name, double Latitude, double Longitude, int RadiusMeters);

public sealed record GeofenceResult(
    GeofenceOutcome Outcome,
    Guid? LocationId = null,
    string? LocationName = null,
    double? DistanceMeters = null,
    int? RadiusMeters = null);

public static class GeofenceEvaluator
{
    public const double MaxUsableAccuracyMeters = 200;

    public static GeofenceResult Evaluate(
        double latitude, double longitude, double accuracyMeters, IReadOnlyList<GeofenceCandidate> candidates)
    {
        if (candidates.Count == 0) return new(GeofenceOutcome.NoCandidates);
        if (accuracyMeters > MaxUsableAccuracyMeters) return new(GeofenceOutcome.AccuracyTooLow);

        var measured = candidates
            .Select(c => new Measured(c, GeoMath.DistanceMeters(latitude, longitude, c.Latitude, c.Longitude)))
            .OrderBy(m => m.Distance)
            .ToList();

        var inside = measured.FirstOrDefault(m => m.Distance + accuracyMeters <= m.Candidate.RadiusMeters);
        if (inside is not null) return Build(GeofenceOutcome.Inside, inside);

        var borderline = measured.FirstOrDefault(m => m.Distance - accuracyMeters <= m.Candidate.RadiusMeters);
        if (borderline is not null) return Build(GeofenceOutcome.Borderline, borderline);

        return Build(GeofenceOutcome.Outside, measured[0]);
    }

    private sealed record Measured(GeofenceCandidate Candidate, double Distance);

    private static GeofenceResult Build(GeofenceOutcome outcome, Measured m) =>
        new(outcome, m.Candidate.LocationId, m.Candidate.Name, m.Distance, m.Candidate.RadiusMeters);
}