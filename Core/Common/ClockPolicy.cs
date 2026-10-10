namespace Core.Common;

public enum ClockEvent { In, Out }

public sealed record ClockDecision(bool Accepted, bool Flagged, string? RejectReason = null)
{
    public static ClockDecision Clean { get; } = new(true, false);
    public static ClockDecision Flag { get; } = new(true, true);
    public static ClockDecision Reject(string reason) => new(false, false, reason);
}
public static class ClockPolicy
{
    public static ClockDecision Decide(ClockEvent evt, GeofenceResult? result, double? accuracyMeters)
    {
        if (result is { Outcome: GeofenceOutcome.Inside }) return ClockDecision.Clean;

        if (evt == ClockEvent.Out) return ClockDecision.Flag;

        return result switch
        {
            null => ClockDecision.Reject("Your location is required to clock in. Allow location access and try again."),
            { Outcome: GeofenceOutcome.Borderline } => ClockDecision.Flag,
            { Outcome: GeofenceOutcome.AccuracyTooLow} => ClockDecision.Reject(
                $"Your GPS signal is too week (accuracy about {Math.Round(accuracyMeters ?? 0)} m). Move outdoors or near a window and try again."),
            { Outcome: GeofenceOutcome.Outside} r => ClockDecision.Reject(
                $"You're about {Math.Round(r.DistanceMeters ?? 0)} m from {r.LocationName}; clocking in is allowed within {r.RadiusMeters} m."),
            _ => ClockDecision.Reject("No work location is available to clock in at. Contact HR.")
        };
    }
}