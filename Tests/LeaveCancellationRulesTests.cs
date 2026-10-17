using Core.Common;
using Core.Enums;

namespace Tests;

public class LeaveCancellationRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 15);
    private static readonly DateOnly Future = new(2026, 11, 2);
    private static readonly DateOnly Past = new(2026, 10, 5);

    private static LeaveCancelDecision Decide(
        ApprovalStatus status, DateOnly start, bool requester = true, bool hr = false) =>
        LeaveCancellationRules.Decide(status, start, Today, requester, hr);

    [Fact]
    public void The_Requester_Can_Withdraw_A_Pending_Request() =>
        Assert.Equal(LeaveCancelKind.Withdraw, Decide(ApprovalStatus.Pending, Future).Kind);

    [Fact]
    public void Someone_Else_Cannot_Withdraw_A_Pending_Request() =>
        Assert.NotNull(Decide(ApprovalStatus.Pending, Future, requester: false, hr: true).Error);

    [Fact]
    public void The_Requester_Can_Cancel_Approved_Leave_Before_It_Starts() =>
        Assert.Equal(LeaveCancelKind.CancelApproved, Decide(ApprovalStatus.Approved, Future).Kind);

    [Fact]
    public void The_Requester_Cannot_Cancel_On_Or_After_The_Start_Date()
    {
        Assert.NotNull(Decide(ApprovalStatus.Approved, Today).Error);   // starts today
        Assert.Contains("only be cancelled by HR", Decide(ApprovalStatus.Approved, Past).Error);
    }

    [Fact]
    public void Hr_Can_Cancel_Approved_Leave_At_Any_Time()
    {
        Assert.Equal(LeaveCancelKind.CancelApproved, Decide(ApprovalStatus.Approved, Past, requester: false, hr: true).Kind);
        Assert.Equal(LeaveCancelKind.CancelApproved, Decide(ApprovalStatus.Approved, Future, requester: false, hr: true).Kind);
    }

    [Fact]
    public void An_Hr_Person_Is_Treated_As_The_Requester_For_Their_Own_Leave()
    {
        // No back door: after the start date they can't hand themselves the days back
        Assert.NotNull(Decide(ApprovalStatus.Approved, Past, requester: true, hr: true).Error);
        Assert.Equal(LeaveCancelKind.CancelApproved, Decide(ApprovalStatus.Approved, Future, requester: true, hr: true).Kind);
    }

    [Fact]
    public void A_Stranger_Cannot_Cancel_Approved_Leave() =>
        Assert.NotNull(Decide(ApprovalStatus.Approved, Future, requester: false, hr: false).Error);

    [Theory]
    [InlineData(ApprovalStatus.Rejected)]
    [InlineData(ApprovalStatus.Cancelled)]
    public void Finished_Requests_Cannot_Be_Cancelled(ApprovalStatus status)
    {
        Assert.NotNull(Decide(status, Future).Error);
        Assert.NotNull(Decide(status, Future, requester: false, hr: true).Error);
    }
}