namespace Core.Common;

public sealed record CorrectionPlan(DateTime ClockInUtc, DateTime? ClockOutUtc, string? Error)
{
    public static CorrectionPlan Fail(string error) => new(default, null, error);
}

public static class CorrectionPlanner
{
    public static CorrectionPlan Plan(
        DateOnly date,
        string? timeZoneId,
        DateTime? existingInUtc,
        DateTime? existingOutUtc,
        TimeOnly? requestedIn,
        TimeOnly? requestedOut,
        DateTime nowUtc)
    {

        if (requestedIn is null && requestedOut is null)
            return CorrectionPlan.Fail("Provide a clock-in time, a clock-out time, or both.");

        if (existingInUtc is null && (requestedIn is null || requestedOut is null))
            return CorrectionPlan.Fail("There's no attendance record for that day, so both a clock-in and a clock-out time are required.");

        DateTime clockIn;
        if (requestedIn is TimeOnly ti)
        {
            var converted = LocalClock.ToUtc(date, ti, timeZoneId);
            if (converted is null) return CorrectionPlan.Fail(NoSuchTime("clock-in"));
            clockIn = converted.Value;
        }
        else clockIn = existingInUtc!.Value;

        var clockOut = existingOutUtc;
        if (requestedOut is TimeOnly to)
        {
            var converted = LocalClock.ToUtc(date, to, timeZoneId);
            if (converted is null) return CorrectionPlan.Fail(NoSuchTime("clock-out"));
            clockOut = converted;
        }

        if (clockOut is DateTime co && co <= clockIn)
            return CorrectionPlan.Fail("Clock-out must be after clock-in on the same day. Overnight shifts aren't supported yet.");

        if (clockIn > nowUtc || clockOut > nowUtc)
            return CorrectionPlan.Fail("Times can't be in the future.");

        return new CorrectionPlan(clockIn, clockOut, null);
    }

    private static string NoSuchTime(string which) =>
        $"The {which} time doesn't exist on that date because of a clock change. Pick another time.";

}