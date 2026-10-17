using Core.Common;
using Core.Enums;

namespace Tests;

public class LeaveApprovalRulesTests
{
    private static string? Check(
        ApprovalStatus status = ApprovalStatus.Pending, EmployeeStatus employee = EmployeeStatus.Regular,
        LeaveEntitlementMode mode = LeaveEntitlementMode.Annual, int leaveYear = 2026, int currentYear = 2026,
        decimal balance = 10, decimal days = 3, bool documentRequired = false, int documents = 0) =>
        LeaveApprovalRules.CheckFinalApproval(status, employee, mode, leaveYear, currentYear, balance, days, documentRequired, documents);

    [Fact] public void An_Ordinary_Request_Is_Approvable() => Assert.Null(Check());

    [Theory]
    [InlineData(ApprovalStatus.Approved)]
    [InlineData(ApprovalStatus.Rejected)]
    [InlineData(ApprovalStatus.Cancelled)]
    public void Only_A_Pending_Request_Is_Approvable(ApprovalStatus status) =>
        Assert.Contains("no longer pending", Check(status: status));

    [Theory]
    [InlineData(EmployeeStatus.Resigned)]
    [InlineData(EmployeeStatus.Terminated)]
    public void Someone_Who_Has_Left_Is_Refused(EmployeeStatus status) =>
        Assert.Contains("status no longer allows", Check(employee: status));

    [Fact] public void A_Missing_Required_Document_Is_Refused() =>
        Assert.Contains("document is required", Check(documentRequired: true, documents: 0));

    [Fact] public void An_Attached_Document_Satisfies_The_Rule() =>
        Assert.Null(Check(documentRequired: true, documents: 1));

    [Fact] public void A_Balance_That_Exactly_Covers_The_Days_Passes() => Assert.Null(Check(balance: 3, days: 3));

    [Fact] public void A_Half_Day_Short_Is_Refused() =>
        Assert.Contains("needs 3.5", Check(balance: 3, days: 3.5m));

    [Fact] public void A_Negative_Balance_Is_Shown_As_Zero() =>
        Assert.Contains("balance is 0 day(s)", Check(balance: -2, days: 1));

    [Fact] public void A_Closed_Leave_Year_Is_Refused() =>
        Assert.Contains("closed leave year", Check(leaveYear: 2025, currentYear: 2026));

    [Fact] public void A_Future_Year_Is_Not_Treated_As_Closed() =>
        Assert.Null(Check(leaveYear: 2027, currentYear: 2026));

    [Fact] public void Unlimited_Types_Skip_The_Balance_And_Year_Checks() =>
        Assert.Null(Check(mode: LeaveEntitlementMode.Unlimited, balance: 0, days: 200, leaveYear: 2024, currentYear: 2026));

    [Fact] public void Unlimited_Types_Still_Need_Their_Document() =>
        Assert.NotNull(Check(mode: LeaveEntitlementMode.Unlimited, documentRequired: true, documents: 0));

    [Fact] public void A_Decided_Request_Is_Reported_Before_Anything_Else() =>
        Assert.Contains("no longer pending", Check(
            status: ApprovalStatus.Approved, employee: EmployeeStatus.Resigned, documentRequired: true, balance: 0));
}