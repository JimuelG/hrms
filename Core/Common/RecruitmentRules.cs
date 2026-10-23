using Core.Entities;
using Core.Enums;

namespace Core.Common;

public sealed record StageCount(string Stage, int Count);
public sealed record StageReach(string Stage, int Reached, decimal? PercentOfApplied, decimal? PercentOfPrevious, bool SmallSample);
public sealed record DurationStats(int Count, decimal? AverageDays, decimal? MedianDays);
public sealed record AcceptanceRate(int Accepted, int Declined, decimal? Percent, bool SmallSample);

public static class RecruitmentRules
{
    public const int SmallSample = 5;
    public const int MaxRangeDays = 366;

    public static readonly ApplicationStatus[] Pipeline =
    [
        ApplicationStatus.Applied,
        ApplicationStatus.Screening,
        ApplicationStatus.HrInterview,
        ApplicationStatus.JobOffer,
        ApplicationStatus.Hired,
    ];

    public static string? CheckRange(DateOnly from, DateOnly to)
    {
        if (from.Year < LeaveCalculator.MinYear || to.Year > LeaveCalculator.MaxYear) return "The dates are out of range.";
        if (to < from) return "The end date can't be before the start date.";
        if (to.DayNumber - from.DayNumber + 1 > MaxRangeDays) return $"Choose at most {MaxRangeDays} days at a time.";
        return null;
    }

    public static IReadOnlyList<StageCount> Pipeline_Counts(IEnumerable<ApplicationStatus> statuses)
    {
        var list = statuses.ToList();
        var rows = Pipeline.Select(s => new StageCount(s.ToString(), list.Count(x => x == s))).ToList();
        rows.AddRange(list.Where(s => !Pipeline.Contains(s)).GroupBy(s => s)
            .OrderBy(g => g.Key).Select(g => new StageCount(g.Key.ToString(), g.Count())));
        return rows;
    }

    public static int StageIndex(ApplicationStatus s) => Array.IndexOf(Pipeline, s);

    public static int FurthestStage(ApplicationStatus current, bool hadInterview, bool hadOffer)
    {
        var idx = StageIndex(current);
        if (idx >= 0) return idx;
        var evidence = 0;
        if (hadInterview) evidence = Math.Max(evidence, StageIndex(ApplicationStatus.HrInterview));
        if (hadOffer) evidence = Math.Max(evidence, StageIndex(ApplicationStatus.JobOffer));
        return evidence;
    }

    public static IReadOnlyList<StageReach> Reach(IEnumerable<int> furthestStages)
    {
        var f = furthestStages.ToList();
        var applied = f.Count;
        var result = new List<StageReach>();
        int? previous = null;

        for (var i = 0; i < Pipeline.Length; i++)
        {
            var reached = f.Count(x => x >= i);
            result.Add(new StageReach(
                Pipeline[i].ToString(),
                reached,
                applied == 0 ? null : Pct(reached, applied),
                previous is int p && p > 0 ? Pct(reached, p) : null,
                (previous ?? applied) < SmallSample));
            previous = reached;
        }

        return result;
    }

    public static decimal Pct(int part, int whole) => decimal.Round(part * 100m / whole, 1, MidpointRounding.AwayFromZero);

    public static DurationStats Durations(IEnumerable<int> days)
    {
        var list = days.Where(d => d >= 0).OrderBy(d => d).ToList();
        if (list.Count == 0) return new(0, null, null);

        var avg = decimal.Round((decimal)list.Sum() / list.Count, 1, MidpointRounding.AwayFromZero);
        var mid = list.Count / 2;
        var median = list.Count % 2 == 1 ? list[mid] : (list[mid - 1] + list[mid]) / 2m;
        return new(list.Count, avg, decimal.Round(median, 1, MidpointRounding.AwayFromZero));
    }

    public static AcceptanceRate Acceptance(int accepted, int declined)
    {
        var answered = accepted + declined;
        return new(accepted, declined, answered == 0 ? null : Pct(accepted, answered), answered < SmallSample);
    }

    public static int DaysBetween(DateOnly from, DateOnly to) => to.DayNumber - from.DayNumber;
}