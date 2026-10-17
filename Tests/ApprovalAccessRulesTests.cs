using Core.Common;
using Core.Enums;
using static Core.Common.ApprovalAccessRules;

namespace Tests;

public class ApprovalAccessRulesTests
{
    private static readonly Guid RequesterEmployee = Guid.NewGuid();
    private static readonly Guid ManagerEmployee = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();

    private static Viewer Requester() => new(Guid.NewGuid(), RequesterEmployee, new HashSet<string>());
    private static Viewer Manager() => new(Guid.NewGuid(), ManagerEmployee, new HashSet<string>());
    private static Viewer Hr(Guid? employeeId = null) => new(Guid.NewGuid(), employeeId, new HashSet<string> { "leave.approve" });
    private static Viewer Nobody() => new(Guid.NewGuid(), Stranger, new HashSet<string>());

    private static StepState ManagerStep(ApprovalStepStatus status) =>
        new(1, ApprovalStepKind.Manager, "Manager approval", null, ManagerEmployee, status, null);

    private static StepState HrStep(ApprovalStepStatus status) =>
        new(2, ApprovalStepKind.Permission, "HR approval", "leave.approve", null, status, null);

    private static IReadOnlyList<StepState> AtManager() => [ManagerStep(ApprovalStepStatus.Pending), HrStep(ApprovalStepStatus.Waiting)];
    private static IReadOnlyList<StepState> AtHr() => [ManagerStep(ApprovalStepStatus.Approved), HrStep(ApprovalStepStatus.Pending)];

    private static ApprovalAccess Access(Viewer v, IReadOnlyList<StepState> steps, ApprovalStatus status = ApprovalStatus.Pending) =>
        Evaluate(v, RequesterEmployee, status, steps);

    [Fact]
    public void The_Requester_Can_See_Their_Request_But_Never_Decide_It()
    {
        var a = Access(Requester(), AtManager());
        Assert.True(a.IsRequester);
        Assert.True(a.CanSeeRequest);
        Assert.False(a.CanDecideNow);
    }

    [Fact]
    public void The_Assigned_Manager_Can_Decide_While_It_Is_Their_Turn()
    {
        var a = Access(Manager(), AtManager());
        Assert.True(a.IsStepApprover);
        Assert.True(a.CanSeeRequest);
        Assert.True(a.CanDecideNow);
    }

    [Fact]
    public void The_Manager_Can_Still_See_It_After_Moving_On_But_Cannot_Decide()
    {
        var a = Access(Manager(), AtHr());
        Assert.True(a.CanSeeRequest);
        Assert.False(a.CanDecideNow);
    }

    [Fact]
    public void Hr_Can_See_A_Request_Still_With_The_Manager_But_Is_Not_Asked_To_Act()   // the key inbox rule
    {
        var a = Access(Hr(), AtManager());
        Assert.True(a.CanSeeRequest);
        Assert.False(a.CanDecideNow);
    }

    [Fact]
    public void Hr_Can_Decide_Once_It_Reaches_The_Hr_Step() =>
        Assert.True(Access(Hr(), AtHr()).CanDecideNow);

    [Fact]
    public void Someone_Who_Is_Not_Involved_Sees_Nothing_And_Cannot_Decide()
    {
        var a = Access(Nobody(), AtManager());
        Assert.False(a.CanSeeRequest);
        Assert.False(a.CanDecideNow);
    }

    [Fact]
    public void A_Person_With_The_Hr_Permission_Who_Is_The_Requester_Cannot_Decide_Their_Own_Request()
    {
        var requesterWithHr = new Viewer(Guid.NewGuid(), RequesterEmployee, new HashSet<string> { "leave.approve" });
        var a = Access(requesterWithHr, AtHr());

        Assert.True(a.IsRequester);
        Assert.False(a.CanDecideNow);
    }

    [Fact]
    public void Nobody_Can_Decide_A_Finished_Request()
    {
        var steps = new[] { ManagerStep(ApprovalStepStatus.Approved), HrStep(ApprovalStepStatus.Approved) };
        Assert.False(Access(Manager(), steps, ApprovalStatus.Approved).CanDecideNow);
        Assert.False(Access(Hr(), steps, ApprovalStatus.Approved).CanDecideNow);
    }

    [Fact]
    public void A_Skipped_Step_Has_No_Approver()
    {
        var skipped = new[] { ManagerStep(ApprovalStepStatus.Skipped), HrStep(ApprovalStepStatus.Pending) };
        Assert.False(Access(Manager(), skipped).CanSeeRequest);
    }

    [Fact]
    public void A_Viewer_With_No_Employee_Record_Is_Matched_By_Permission_Only()
    {
        Assert.True(Access(Hr(employeeId: null), AtHr()).CanDecideNow);
        Assert.False(Access(new Viewer(Guid.NewGuid(), null, new HashSet<string>()), AtManager()).CanSeeRequest);
    }

    [Fact]
    public void A_Rejection_Needs_A_Reason()
    {
        Assert.NotNull(CheckNote(ApprovalEventType.Rejected, null).Error);
        Assert.NotNull(CheckNote(ApprovalEventType.Rejected, "   ").Error);
        Assert.Null(CheckNote(ApprovalEventType.Rejected, " Not enough cover ").Error);
    }

    [Fact]
    public void A_Comment_Needs_Text() =>
        Assert.NotNull(CheckNote(ApprovalEventType.Commented, "").Error);

    [Fact]
    public void An_Approval_Note_Is_Optional()
    {
        var r = CheckNote(ApprovalEventType.StepApproved, null);
        Assert.Null(r.Error);
        Assert.Null(r.Note);
    }

    [Fact]
    public void Notes_Are_Trimmed_And_Length_Checked()
    {
        Assert.Equal("ok", CheckNote(ApprovalEventType.StepApproved, "  ok  ").Note);
        Assert.NotNull(CheckNote(ApprovalEventType.StepApproved, new string('x', 501)).Error);
        Assert.Null(CheckNote(ApprovalEventType.StepApproved, new string('x', 500)).Error);
    }
}