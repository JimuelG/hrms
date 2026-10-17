using Core.Common;
using Core.Enums;

namespace Tests;

public class ApprovalStateMachineTests
{
    private static StepState S(int order, ApprovalStepStatus status, ApprovalStepKind kind = ApprovalStepKind.Manager) =>
        new(order, kind, $"Step {order}", kind == ApprovalStepKind.Permission ? "leave.approve" : null, null, status, null);

    private static IReadOnlyList<StepState> Pair(ApprovalStepStatus first, ApprovalStepStatus second) =>
        [S(1, first), S(2, second, ApprovalStepKind.Permission)];

    private static ApprovalStepStatus StatusAt(StepOutcome o, int order) => o.Steps.Single(s => s.Order == order).Status;

    [Fact]
    public void Approving_The_First_Step_Moves_The_Request_To_The_Second()
    {
        var o = ApprovalStateMachine.Approve(Pair(ApprovalStepStatus.Pending, ApprovalStepStatus.Waiting), 1);

        Assert.Null(o.Error);
        Assert.Equal(ApprovalStatus.Pending, o.RequestStatus);
        Assert.False(o.IsFinal);
        Assert.Equal(2, o.ActiveStepOrder);
        Assert.Equal(ApprovalStepStatus.Approved, StatusAt(o, 1));
        Assert.Equal(ApprovalStepStatus.Pending, StatusAt(o, 2));
    }

    [Fact]
    public void Approving_The_Last_Step_Finishes_The_Request()
    {
        var o = ApprovalStateMachine.Approve(Pair(ApprovalStepStatus.Approved, ApprovalStepStatus.Pending), 2);

        Assert.Equal(ApprovalStatus.Approved, o.RequestStatus);
        Assert.True(o.IsFinal);
        Assert.Null(o.ActiveStepOrder);
    }

    [Fact]
    public void A_Skipped_First_Step_Means_One_Approval_Finishes_The_Request()
    {
        var o = ApprovalStateMachine.Approve(Pair(ApprovalStepStatus.Skipped, ApprovalStepStatus.Pending), 2);
        Assert.Equal(ApprovalStatus.Approved, o.RequestStatus);
        Assert.True(o.IsFinal);
    }

    [Fact]
    public void A_Skipped_Last_Step_Means_The_First_Approval_Finishes_The_Request()
    {
        var o = ApprovalStateMachine.Approve(Pair(ApprovalStepStatus.Pending, ApprovalStepStatus.Skipped), 1);
        Assert.Equal(ApprovalStatus.Approved, o.RequestStatus);
        Assert.True(o.IsFinal);
    }

    [Fact]
    public void A_Skipped_Middle_Step_Is_Stepped_Over()
    {
        var steps = new[]
        {
            S(1, ApprovalStepStatus.Pending), S(2, ApprovalStepStatus.Skipped), S(3, ApprovalStepStatus.Waiting)
        };
        var o = ApprovalStateMachine.Approve(steps, 1);

        Assert.Equal(3, o.ActiveStepOrder);
        Assert.Equal(ApprovalStepStatus.Skipped, StatusAt(o, 2));
        Assert.Equal(ApprovalStepStatus.Pending, StatusAt(o, 3));
    }

    [Fact]
    public void A_Rejection_Ends_The_Request_And_Voids_What_Is_Left()
    {
        var o = ApprovalStateMachine.Reject(Pair(ApprovalStepStatus.Pending, ApprovalStepStatus.Waiting), 1);

        Assert.Equal(ApprovalStatus.Rejected, o.RequestStatus);
        Assert.True(o.IsFinal);
        Assert.Equal(ApprovalStepStatus.Rejected, StatusAt(o, 1));
        Assert.Equal(ApprovalStepStatus.Voided, StatusAt(o, 2));
    }

    [Fact]
    public void A_Rejection_At_The_Second_Step_Keeps_The_First_Approval_On_Record()
    {
        var o = ApprovalStateMachine.Reject(Pair(ApprovalStepStatus.Approved, ApprovalStepStatus.Pending), 2);

        Assert.Equal(ApprovalStatus.Rejected, o.RequestStatus);
        Assert.Equal(ApprovalStepStatus.Approved, StatusAt(o, 1));
        Assert.Equal(ApprovalStepStatus.Rejected, StatusAt(o, 2));
    }

    [Fact]
    public void A_Rejection_Does_Not_Touch_Steps_That_Were_Skipped()
    {
        var steps = new[] { S(1, ApprovalStepStatus.Skipped), S(2, ApprovalStepStatus.Pending, ApprovalStepKind.Permission) };
        Assert.Equal(ApprovalStepStatus.Skipped, StatusAt(ApprovalStateMachine.Reject(steps, 2), 1));
    }

    [Fact]
    public void A_Later_Step_Cannot_Be_Decided_Before_Its_Turn()
    {
        var o = ApprovalStateMachine.Approve(Pair(ApprovalStepStatus.Pending, ApprovalStepStatus.Waiting), 2);

        Assert.NotNull(o.Error);
        Assert.Equal(ApprovalStatus.Pending, o.RequestStatus);
        Assert.Equal(ApprovalStepStatus.Waiting, StatusAt(o, 2));
    }

    [Fact]
    public void An_Earlier_Step_Cannot_Be_Decided_Twice()
    {
        var o = ApprovalStateMachine.Approve(Pair(ApprovalStepStatus.Approved, ApprovalStepStatus.Pending), 1);
        Assert.NotNull(o.Error);
    }

    [Fact]
    public void A_Finished_Request_Cannot_Be_Decided_Again()
    {
        var approved = Pair(ApprovalStepStatus.Approved, ApprovalStepStatus.Approved);
        Assert.NotNull(ApprovalStateMachine.Approve(approved, 2).Error);
        Assert.NotNull(ApprovalStateMachine.Reject(approved, 2).Error);
    }

    [Fact]
    public void A_Step_That_Does_Not_Exist_Cannot_Be_Decided() =>
        Assert.NotNull(ApprovalStateMachine.Approve(Pair(ApprovalStepStatus.Pending, ApprovalStepStatus.Waiting), 9).Error);

    [Fact]
    public void Cancelling_Voids_Everything_Still_Open()
    {
        var o = ApprovalStateMachine.Cancel(Pair(ApprovalStepStatus.Approved, ApprovalStepStatus.Pending));

        Assert.Null(o.Error);
        Assert.Equal(ApprovalStatus.Cancelled, o.RequestStatus);
        Assert.Equal(ApprovalStepStatus.Approved, StatusAt(o, 1));   // a decision already made stays on record
        Assert.Equal(ApprovalStepStatus.Voided, StatusAt(o, 2));
    }

    [Fact]
    public void A_Decided_Request_Cannot_Be_Cancelled()
    {
        Assert.NotNull(ApprovalStateMachine.Cancel(Pair(ApprovalStepStatus.Approved, ApprovalStepStatus.Approved)).Error);
        Assert.NotNull(ApprovalStateMachine.Cancel(Pair(ApprovalStepStatus.Rejected, ApprovalStepStatus.Voided)).Error);
    }

    [Fact]
    public void The_Input_Is_Never_Mutated()
    {
        var steps = Pair(ApprovalStepStatus.Pending, ApprovalStepStatus.Waiting);
        _ = ApprovalStateMachine.Approve(steps, 1);

        Assert.Equal(ApprovalStepStatus.Pending, steps[0].Status);
        Assert.Equal(ApprovalStepStatus.Waiting, steps[1].Status);
    }

    [Theory]
    [InlineData(ApprovalStepStatus.Pending, ApprovalStepStatus.Waiting, ApprovalStatus.Pending)]
    [InlineData(ApprovalStepStatus.Approved, ApprovalStepStatus.Pending, ApprovalStatus.Pending)]
    [InlineData(ApprovalStepStatus.Approved, ApprovalStepStatus.Approved, ApprovalStatus.Approved)]
    [InlineData(ApprovalStepStatus.Skipped, ApprovalStepStatus.Approved, ApprovalStatus.Approved)]
    [InlineData(ApprovalStepStatus.Rejected, ApprovalStepStatus.Voided, ApprovalStatus.Rejected)]
    [InlineData(ApprovalStepStatus.Approved, ApprovalStepStatus.Voided, ApprovalStatus.Cancelled)]
    public void The_Overall_Status_Follows_From_The_Steps(
        ApprovalStepStatus first, ApprovalStepStatus second, ApprovalStatus expected) =>
        Assert.Equal(expected, ApprovalStateMachine.StatusOf(Pair(first, second)));
}