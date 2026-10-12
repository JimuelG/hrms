namespace Core.Common;

public sealed record OvertimeCheck(int Minutes, string? Error);

public class OvertimeRules
{
    public const int MinMinutes = 15;
    public const int MaxMinutes = 720;

    public static OvertimeCheck Measure(TimeOnly start, TimeOnly end)
    {
        if (end <= start)
            return new(0, "End time must be after start time. Overtime that crosses midnight isn't supported yet; file it as two requests.");

        var minutes = (int)(end - start).TotalMinutes;
        if (minutes < MinMinutes) return new(minutes, $"Overtime must be at least {MinMinutes} minutes.");
        if (minutes > MaxMinutes) return new(minutes, $"Overtime can't exceed {MaxMinutes / 60} hours in one request.");

        return new(minutes, null);
    }

    public static bool Overlaps(TimeOnly aStart, TimeOnly aEnd, TimeOnly bStart, TimeOnly bEnd) =>
        aStart < bEnd && bStart < aEnd;
}