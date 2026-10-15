using Core.Common;
using Core.Enums;

namespace Tests;

public class LeaveGrantPlannerTests
{
    private static readonly LeavePolicy Vacation = new(15, true, 5, 0);

    private static DateOnly D(int y, int m, int d) => new(y, m, d);
    private static LedgerRow Row(int year, LeaveEntryType type, decimal days, bool auto = true) => new(year, type, days, auto);
    private static (int, LeaveEntryType, decimal)[] Shape(IEnumerable<PlannedEntry> plan) =>
        plan.Select(p => (p.Year, p.Type, p.Days)).ToArray();

    [Theory]
    [InlineData(15, 2026, 1, 1, 2026, 15)]
    [InlineData(15, 2025, 6, 1, 2026, 15)]     // eligible before the year began: full
    [InlineData(15, 2026, 7, 1, 2026, 7.5)]    // 184/365 of 15 = 7.56, to the nearest half-day
    [InlineData(15, 2026, 10, 1, 2026, 4)]     // 92/365 of 15 = 3.78
    [InlineData(10, 2026, 12, 31, 2026, 0)]    // one day left
    [InlineData(12, 2024, 7, 1, 2024, 6)]      // leap year: 184/366 of 12 = 6.03
    [InlineData(15, 2027, 1, 1, 2026, 0)]      // basis after the year
    public void Prorate_Counts_Remaining_Days_To_The_Nearest_Half_Day(
        double full, int y, int m, int d, int year, double expected) =>
        Assert.Equal((decimal)expected, LeaveGrantPlanner.Prorate((decimal)full, D(y, m, d), year));

    [Fact]
    public void Eligibility_Is_Hire_Date_Plus_Service_Months() =>
        Assert.Equal(D(2027, 3, 1), LeaveGrantPlanner.EligibleFrom(D(2026, 3, 1), 12));

    [Fact]
    public void Eligibility_Clamps_To_The_End_Of_A_Short_Month() =>
        Assert.Equal(D(2026, 2, 28), LeaveGrantPlanner.EligibleFrom(D(2026, 1, 31), 1));

    [Fact]
    public void A_New_Hire_Gets_A_Prorated_First_Year_And_Nothing_Carries_Over()
    {
        var plan = LeaveGrantPlanner.Plan(Vacation, D(2026, 7, 1), D(2026, 10, 15), []);
        Assert.Equal([(2026, LeaveEntryType.Grant, 7.5m)], Shape(plan));
        Assert.Contains("prorated from 2026-07-01", plan[0].Description);
    }

    [Fact]
    public void A_Hire_On_January_First_Is_Not_Described_As_Prorated()
    {
        var plan = LeaveGrantPlanner.Plan(Vacation, D(2026, 1, 1), D(2026, 3, 1), []);
        Assert.Equal([(2026, LeaveEntryType.Grant, 15m)], Shape(plan));
        Assert.DoesNotContain("prorated", plan[0].Description);
    }

    [Fact]
    public void Proration_Can_Be_Switched_Off() =>
        Assert.Equal([(2026, LeaveEntryType.Grant, 15m)],
            Shape(LeaveGrantPlanner.Plan(new LeavePolicy(15, false, 5, 0), D(2026, 7, 1), D(2026, 10, 15), [])));

    [Fact]
    public void A_Hire_On_The_Last_Day_Still_Records_A_Zero_Day_Grant()
    {
        // The row is the marker that the year was granted, so it exists even at zero
        var plan = LeaveGrantPlanner.Plan(Vacation, D(2026, 12, 31), D(2026, 12, 31), []);
        Assert.Equal([(2026, LeaveEntryType.Grant, 0m)], Shape(plan));
    }

    [Fact]
    public void A_Long_Serving_Employee_Starts_At_The_Current_Year_With_The_Full_Entitlement() =>
        Assert.Equal([(2026, LeaveEntryType.Grant, 15m)],
            Shape(LeaveGrantPlanner.Plan(Vacation, D(2020, 1, 15), D(2026, 3, 1), [])));

    [Fact]
    public void Planning_Is_Idempotent_Once_The_Year_Is_Granted() =>
        Assert.Empty(LeaveGrantPlanner.Plan(Vacation, D(2020, 1, 15), D(2026, 10, 1), [Row(2026, LeaveEntryType.Grant, 15)]));

    [Fact]
    public void A_Manually_Posted_Grant_Does_Not_Count_As_The_Automatic_One() =>
        Assert.Equal([(2026, LeaveEntryType.Grant, 15m)],
            Shape(LeaveGrantPlanner.Plan(Vacation, D(2020, 1, 15), D(2026, 3, 1),
                [Row(2026, LeaveEntryType.Grant, 15, auto: false)])));

    [Fact]
    public void Unused_Days_Carry_Over_Up_To_The_Cap()
    {
        var existing = new[] { Row(2026, LeaveEntryType.Grant, 15), Row(2026, LeaveEntryType.Usage, -6, auto: false) };   // 9 left
        Assert.Equal([(2027, LeaveEntryType.Grant, 15m), (2027, LeaveEntryType.CarryOver, 5m)],
            Shape(LeaveGrantPlanner.Plan(Vacation, D(2020, 1, 15), D(2027, 1, 5), existing)));
    }

    [Fact]
    public void Carry_Over_Is_The_Leftover_When_It_Is_Below_The_Cap()
    {
        var existing = new[] { Row(2026, LeaveEntryType.Grant, 15), Row(2026, LeaveEntryType.Usage, -12, auto: false) };   // 3 left
        Assert.Equal([(2027, LeaveEntryType.Grant, 15m), (2027, LeaveEntryType.CarryOver, 3m)],
            Shape(LeaveGrantPlanner.Plan(Vacation, D(2020, 1, 15), D(2027, 1, 5), existing)));
    }

    [Fact]
    public void A_Negative_Leftover_Carries_Nothing()
    {
        var existing = new[] { Row(2026, LeaveEntryType.Grant, 15), Row(2026, LeaveEntryType.Usage, -17, auto: false) };
        Assert.Equal([(2027, LeaveEntryType.Grant, 15m)],
            Shape(LeaveGrantPlanner.Plan(Vacation, D(2020, 1, 15), D(2027, 1, 5), existing)));
    }

    [Fact]
    public void A_Cap_Of_Zero_Means_Nothing_Carries_Over() =>
        Assert.Equal([(2027, LeaveEntryType.Grant, 10m)],
            Shape(LeaveGrantPlanner.Plan(new LeavePolicy(10, true, 0, 0), D(2020, 1, 15), D(2027, 2, 1),
                [Row(2026, LeaveEntryType.Grant, 10)])));

    [Fact]
    public void Skipped_Years_Are_Created_In_Order_So_Carry_Over_Still_Chains()
    {
        var plan = LeaveGrantPlanner.Plan(Vacation, D(2020, 1, 15), D(2026, 2, 1), [Row(2024, LeaveEntryType.Grant, 15)]);
        Assert.Equal(
        [
            (2025, LeaveEntryType.Grant, 15m), (2025, LeaveEntryType.CarryOver, 5m),
            (2026, LeaveEntryType.Grant, 15m), (2026, LeaveEntryType.CarryOver, 5m)
        ], Shape(plan));
    }

    [Fact]
    public void Nothing_Is_Granted_Before_The_Minimum_Service_Is_Reached() =>
        Assert.Empty(LeaveGrantPlanner.Plan(new LeavePolicy(5, false, 0, 12), D(2026, 3, 1), D(2026, 10, 1), []));

    [Fact]
    public void The_Grant_Appears_Once_The_Minimum_Service_Is_Reached() =>
        Assert.Equal([(2027, LeaveEntryType.Grant, 5m)],
            Shape(LeaveGrantPlanner.Plan(new LeavePolicy(5, false, 0, 12), D(2026, 3, 1), D(2027, 3, 1), [])));

    [Fact]
    public void Reaching_The_Minimum_Service_Mid_Year_Prorates_From_That_Date() =>
        // eligible 2026-09-01: 122 days left, 12 x 122/365 = 4.01, to the nearest half-day
        Assert.Equal([(2026, LeaveEntryType.Grant, 4m)],
            Shape(LeaveGrantPlanner.Plan(new LeavePolicy(12, true, 0, 6), D(2026, 3, 1), D(2026, 10, 15), [])));

    [Fact]
    public void A_Future_Hire_Date_Grants_Nothing() =>
        Assert.Empty(LeaveGrantPlanner.Plan(Vacation, D(2027, 1, 10), D(2026, 10, 15), []));
}