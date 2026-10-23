using Core.Common;

namespace Tests;

public class AlertRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 22);
    private static readonly DateTime Now = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

    [Fact] public void An_Approval_Is_Stale_At_Exactly_The_Threshold() =>
        Assert.True(AlertRules.IsStaleApproval(Now.AddDays(-3), Now, 3));

    [Fact] public void An_Approval_Just_Under_The_Threshold_Is_Not_Stale() =>
        Assert.False(AlertRules.IsStaleApproval(Now.AddDays(-3).AddMinutes(1), Now, 3));

    [Fact] public void Recent_Activity_Is_Not_Stale() => Assert.False(AlertRules.IsStaleApproval(Now.AddHours(-2), Now, 3));

    [Fact] public void No_Date_Means_No_Document_Alert() =>
        Assert.Equal(DocumentExpiryState.None, AlertRules.DocumentState(null, Today, 30));

    [Theory]
    [InlineData(2026, 10, 21, DocumentExpiryState.Expired)]    // yesterday
    [InlineData(2026, 10, 22, DocumentExpiryState.Expiring)]   // still valid today
    [InlineData(2026, 11, 21, DocumentExpiryState.Expiring)]   // exactly 30 days ahead
    [InlineData(2026, 11, 22, DocumentExpiryState.None)]       // 31 days ahead
    public void Documents_Are_Classified_By_Their_Date(int y, int m, int d, DocumentExpiryState expected) =>
        Assert.Equal(expected, AlertRules.DocumentState(new DateOnly(y, m, d), Today, 30));

    [Fact] public void An_Open_Record_Becomes_An_Alert_After_Two_Days()
    {
        Assert.True(AlertRules.IsOpenAttendance(Today.AddDays(-2), false, Today, 2));
        Assert.False(AlertRules.IsOpenAttendance(Today.AddDays(-1), false, Today, 2));
        Assert.False(AlertRules.IsOpenAttendance(Today, false, Today, 2));
    }

    [Fact] public void A_Record_With_A_Clock_Out_Is_Never_Open() =>
        Assert.False(AlertRules.IsOpenAttendance(Today.AddDays(-30), true, Today, 2));

    [Fact] public void Keys_Differ_By_Kind_And_Document_State()
    {
        var id = Guid.NewGuid();
        Assert.NotEqual(AlertRules.DocumentKey(id, DocumentExpiryState.Expiring), AlertRules.DocumentKey(id, DocumentExpiryState.Expired));
        Assert.NotEqual(AlertRules.ApprovalKey(id), AlertRules.AttendanceKey(id));
    }

    [Fact] public void Reconcile_Creates_The_Missing_And_Resolves_The_Gone()
    {
        var plan = AlertRules.Reconcile(["a", "b"], ["b", "c"]);
        Assert.Equal(["c"], plan.Create);
        Assert.Equal(["a"], plan.Resolve);
    }

    [Fact] public void Reconcile_Is_Idempotent_When_Nothing_Changed()
    {
        var plan = AlertRules.Reconcile(["a", "b"], ["a", "b"]);
        Assert.Empty(plan.Create);
        Assert.Empty(plan.Resolve);
    }

    [Fact] public void Duplicate_Keys_Are_Collapsed() =>
        Assert.Equal(["x"], AlertRules.Reconcile([], ["x", "x"]).Create);

    [Fact] public void Everything_Is_Resolved_When_Nothing_Is_Desired() =>
        Assert.Equal(["a", "b"], AlertRules.Reconcile(["b", "a"], []).Resolve);
}