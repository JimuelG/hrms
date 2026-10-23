using Core.Common;
using Core.Enums;

namespace Tests;

public class RecruitmentRulesTests
{
    private static DateOnly D(int m, int d) => new(2026, m, d);

    // ---- range ----
    [Fact] public void A_Normal_Range_Is_Accepted() => Assert.Null(RecruitmentRules.CheckRange(D(1, 1), D(12, 31)));
    [Fact] public void An_Inverted_Range_Is_Refused() =>
        Assert.Contains("before the start", RecruitmentRules.CheckRange(D(5, 1), D(4, 1)));
    [Fact] public void More_Than_A_Year_Is_Refused() =>
        Assert.Contains("at most 366", RecruitmentRules.CheckRange(new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 2)));

    // ---- pipeline counts ----
    [Fact] public void Every_Stage_Is_Listed_Even_When_Empty()
    {
        var rows = RecruitmentRules.Pipeline_Counts([ApplicationStatus.Applied, ApplicationStatus.Applied]);
        Assert.Equal(RecruitmentRules.Pipeline.Length, rows.Count);
        Assert.Equal(2, rows[0].Count);
        Assert.All(rows.Skip(1), r => Assert.Equal(0, r.Count));
    }

    [Fact] public void Stages_Are_In_Pipeline_Order() =>
        Assert.Equal(RecruitmentRules.Pipeline.Select(s => s.ToString()),
            RecruitmentRules.Pipeline_Counts([]).Select(r => r.Stage));

    // ---- furthest stage ----
    [Fact] public void A_Pipeline_Status_Is_Its_Own_Stage() =>
        Assert.Equal(2, RecruitmentRules.FurthestStage(ApplicationStatus.HrInterview, false, false));

    [Fact] public void Hired_Is_The_Last_Stage() =>
        Assert.Equal(RecruitmentRules.Pipeline.Length - 1, RecruitmentRules.FurthestStage(ApplicationStatus.Hired, false, false));

    [Fact] public void A_Rejected_Application_With_No_Evidence_Only_Applied() =>
        Assert.Equal(0, RecruitmentRules.FurthestStage(ApplicationStatus.Rejected, false, false));

    [Fact] public void A_Rejected_Application_With_An_Interview_Reached_The_Interview_Stage() =>
        Assert.Equal(2, RecruitmentRules.FurthestStage(ApplicationStatus.Rejected, true, false));

    [Fact] public void An_Offer_Is_Further_Evidence_Than_An_Interview() =>
        Assert.Equal(3, RecruitmentRules.FurthestStage(ApplicationStatus.Withdrawn, true, true));

    // ---- reach and conversion ----
    [Fact] public void Reach_Counts_Everyone_Who_Got_At_Least_That_Far()
    {
        // 10 applied; 6 reached screening or beyond; 3 interview or beyond; 1 offer; 1 hired
        var furthest = new[] { 0, 0, 0, 0, 1, 1, 1, 2, 2, 4 };
        var r = RecruitmentRules.Reach(furthest);

        Assert.Equal([10, 6, 3, 1, 1], r.Select(x => x.Reached));
        Assert.Equal(100m, r[0].PercentOfApplied);
        Assert.Equal(60m, r[1].PercentOfApplied);
        Assert.Equal(50m, r[2].PercentOfPrevious);     // 3 of the 6 who reached screening
        Assert.Equal(33.3m, r[3].PercentOfPrevious);   // 1 of the 3 who reached interview
    }

    [Fact] public void No_Applications_Gives_No_Percentages()
    {
        var r = RecruitmentRules.Reach([]);
        Assert.All(r, x => Assert.Null(x.PercentOfApplied));
        Assert.All(r, x => Assert.Equal(0, x.Reached));
    }

    [Fact] public void A_Stage_Nobody_Reached_Has_No_Conversion_From_It()
    {
        var r = RecruitmentRules.Reach([0, 0, 0, 0, 0, 0]);
        Assert.Equal(0m, r[1].PercentOfApplied);
        Assert.Null(r[2].PercentOfPrevious);           // nobody reached the previous stage, so no division
    }

    [Fact] public void Small_Samples_Are_Flagged()
    {
        Assert.True(RecruitmentRules.Reach([0, 1, 2])[0].SmallSample);
        Assert.False(RecruitmentRules.Reach(Enumerable.Repeat(0, 10))[0].SmallSample);
    }

    // ---- durations ----
    [Fact] public void An_Odd_Count_Uses_The_Middle_Value()
    {
        var s = RecruitmentRules.Durations([10, 30, 20]);
        Assert.Equal((3, 20m, 20m), (s.Count, s.AverageDays, s.MedianDays));
    }

    [Fact] public void An_Even_Count_Averages_The_Two_Middle_Values()
    {
        var s = RecruitmentRules.Durations([10, 20, 30, 100]);
        Assert.Equal(40m, s.AverageDays);
        Assert.Equal(25m, s.MedianDays);
    }

    [Fact] public void An_Outlier_Moves_The_Average_But_Not_The_Median()
    {
        var s = RecruitmentRules.Durations([10, 11, 12, 13, 300]);
        Assert.Equal(69.2m, s.AverageDays);
        Assert.Equal(12m, s.MedianDays);
    }

    [Fact] public void Nothing_Gives_No_Statistics()
    {
        var s = RecruitmentRules.Durations([]);
        Assert.Equal(0, s.Count);
        Assert.Null(s.AverageDays);
        Assert.Null(s.MedianDays);
    }

    [Fact] public void Negative_Durations_Are_Ignored() =>
        Assert.Equal(2, RecruitmentRules.Durations([-5, 10, 20]).Count);

    [Fact] public void Days_Between_Counts_Whole_Days_And_Can_Be_Zero()
    {
        Assert.Equal(14, RecruitmentRules.DaysBetween(D(10, 1), D(10, 15)));
        Assert.Equal(0, RecruitmentRules.DaysBetween(D(10, 1), D(10, 1)));
    }

    // ---- acceptance ----
    [Fact] public void Acceptance_Is_Accepted_Over_Answered()
    {
        var a = RecruitmentRules.Acceptance(3, 1);
        Assert.Equal(75m, a.Percent);
        Assert.True(a.SmallSample);
    }

    [Fact] public void No_Answered_Offers_Gives_No_Rate() => Assert.Null(RecruitmentRules.Acceptance(0, 0).Percent);

    [Fact] public void Five_Answers_Is_No_Longer_A_Small_Sample() =>
        Assert.False(RecruitmentRules.Acceptance(4, 1).SmallSample);

    [Fact] public void Percentages_Round_To_One_Decimal() => Assert.Equal(66.7m, RecruitmentRules.Pct(2, 3));
}