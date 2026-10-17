using Core.Common;
using Core.Enums;

namespace Tests;

public class LeaveFilingRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 15);
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    // ---- dates ----
    [Fact]
    public void A_Future_Date_In_The_Current_Year_Is_Fine() =>
        Assert.Null(LeaveFilingRules.CheckDates(D(2026, 11, 2), D(2026, 11, 6), Today));

    [Fact]
    public void A_Request_Cannot_Span_Two_Years() =>
        Assert.Contains("two leave years", LeaveFilingRules.CheckDates(D(2026, 12, 29), D(2027, 1, 3), Today));

    [Fact]
    public void A_Closed_Year_Is_Refused()
    {
        var error = LeaveFilingRules.CheckDates(D(2026, 12, 30), D(2026, 12, 31), D(2027, 1, 5));
        Assert.Contains("closed leave year", error);
    }

    [Fact]
    public void Thirty_Days_Back_Is_The_Limit()
    {
        Assert.Null(LeaveFilingRules.CheckDates(D(2026, 9, 15), D(2026, 9, 15), Today));
        Assert.Contains("30 days", LeaveFilingRules.CheckDates(D(2026, 9, 14), D(2026, 9, 14), Today));
    }

    [Fact]
    public void A_Year_Ahead_Is_The_Limit()
    {
        Assert.Null(LeaveFilingRules.CheckDates(D(2027, 10, 14), D(2027, 10, 15), Today));
        Assert.Contains("a year ahead", LeaveFilingRules.CheckDates(D(2027, 10, 16), D(2027, 10, 16), Today));
    }

    // ---- eligibility ----
    [Theory]
    [InlineData(EmployeeStatus.Resigned)]
    [InlineData(EmployeeStatus.Terminated)]
    [InlineData(EmployeeStatus.Applicant)]
    public void Someone_Who_Is_Not_Employed_Cannot_File(EmployeeStatus status) =>
        Assert.NotNull(LeaveFilingRules.CheckEligibility(status, D(2020, 1, 1), 0, "Vacation Leave", D(2026, 11, 2)));

    [Fact]
    public void Minimum_Service_Is_Measured_To_The_Start_Date()
    {
        var hire = D(2026, 3, 1);
        Assert.Contains("eligible from 2027-03-01",
            LeaveFilingRules.CheckEligibility(EmployeeStatus.Regular, hire, 12, "Service Incentive Leave", D(2027, 2, 28)));
        Assert.Null(LeaveFilingRules.CheckEligibility(EmployeeStatus.Regular, hire, 12, "Service Incentive Leave", D(2027, 3, 1)));
    }

    [Fact]
    public void No_Minimum_Service_Means_Eligible_Immediately() =>
        Assert.Null(LeaveFilingRules.CheckEligibility(EmployeeStatus.Probationary, D(2026, 10, 1), 0, "Vacation Leave", D(2026, 11, 2)));

    // ---- balance ----
    [Fact]
    public void A_Request_Within_The_Available_Balance_Passes() =>
        Assert.Null(LeaveFilingRules.CheckBalance(LeaveEntitlementMode.Annual, 10, 4, 6));

    [Fact]
    public void Days_Held_By_Pending_Requests_Reduce_What_Is_Available()
    {
        var error = LeaveFilingRules.CheckBalance(LeaveEntitlementMode.Annual, 12, 10, 10);
        Assert.Contains("2 day(s) available", error);
        Assert.Contains("10 already requested", error);
    }

    [Fact]
    public void A_Half_Day_Over_The_Balance_Is_Refused() =>
        Assert.NotNull(LeaveFilingRules.CheckBalance(LeaveEntitlementMode.Annual, 10, 4, 6.5m));

    [Fact]
    public void Unlimited_Types_Are_Never_Checked() =>
        Assert.Null(LeaveFilingRules.CheckBalance(LeaveEntitlementMode.Unlimited, 0, 0, 300));

    [Fact]
    public void An_Hr_Granted_Type_With_No_Grant_Is_Refused() =>
        Assert.NotNull(LeaveFilingRules.CheckBalance(LeaveEntitlementMode.HrGranted, 0, 0, 1));

    [Fact]
    public void A_Negative_Balance_Is_Shown_As_Zero_Available() =>
        Assert.Contains("0 day(s) available", LeaveFilingRules.CheckBalance(LeaveEntitlementMode.Annual, -2, 0, 1));

    [Fact]
    public void Annual_Leave_In_A_Future_Year_Is_Explained_Not_Refused_As_A_Balance_Problem() =>
        Assert.Contains("opens on January 1, 2027",
            LeaveFilingRules.CheckFutureYear(LeaveEntitlementMode.Annual, 2027, 2026));

    [Theory]
    [InlineData(LeaveEntitlementMode.Annual, 2026)]
    [InlineData(LeaveEntitlementMode.HrGranted, 2027)]
    [InlineData(LeaveEntitlementMode.Unlimited, 2027)]
    public void Other_Cases_Are_Not_Affected(LeaveEntitlementMode mode, int year) =>
        Assert.Null(LeaveFilingRules.CheckFutureYear(mode, year, 2026));
}