namespace Core.Common;
public sealed record SalarySegment(DateOnly Start, DateOnly End, decimal Monthly)
{
    public int Days => End.DayNumber - Start.DayNumber + 1;
}

public static class SalarySegmenter
{
    public static IReadOnlyList<SalarySegment> Split(IEnumerable<SalaryPoint> history, DateOnly windowStart, DateOnly windowEnd)
    {
        var points = history.ToList();
        if (windowEnd < windowStart) return [];

        var opening = CompensationRules.MonthlyOn(points, windowStart);
        if (opening is null) return [];

        var changes = points
            .Where(p => p.EffectiveForm > windowStart && p.EffectiveForm <= windowEnd)
            .OrderBy(p => p.EffectiveForm)
            .ToList();

        var segments = new List<SalarySegment>();
        var start = windowStart;
        var monthly = opening.Value;

        foreach (var change in changes)
        {
            segments.Add(new SalarySegment(start, change.EffectiveForm.AddDays(-1), monthly));
            start = change.EffectiveForm;
            monthly = change.MonthlyBasic;
        }

        segments.Add(new SalarySegment(start, windowEnd, monthly));
        return segments;
    }
}