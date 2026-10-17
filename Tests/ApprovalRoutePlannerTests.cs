using Core.Common;
using Core.Enums;

namespace Tests;

public class ApprovalRoutePlannerTests
{
    private static readonly Guid Requester = Guid.NewGuid();
    private static readonly Guid RequesterUser = Guid.NewGuid();
    private static readonly Guid Manager = Guid.NewGuid();
    private static readonly Guid HrUser = Guid.NewGuid();

    private static readonly IReadOnlyList<RouteStepTemplate> Route = ApprovalRoutes.For(ApprovalSubjectType.LeaveRequest);

    private static RequesterContext Context(
        Guid? manager, bool employed = true, bool login = true, Guid? employeeId = null, Guid? userId = null) =>
        new(employeeId ?? Requester, userId ?? RequesterUser, manager, employed, login);

    private static PermissionHolders HrHolders(params Guid[] userIds) =>
        new(new Dictionary<string, IReadOnlySet<Guid>> { [ApprovalRoutes.LeaveHrPermission] = userIds.ToHashSet() });

    private static PlannedStep Step(RoutePlan plan, int order) => plan.Steps.Single(s => s.Order == order);

    [Fact]
    public void The_Leave_Route_Is_Manager_Then_Hr()
    {
        Assert.Equal(2, Route.Count);
        Assert.Equal(ApprovalStepKind.Manager, Route[0].Kind);
        Assert.Equal(ApprovalStepKind.Permission, Route[1].Kind);
        Assert.Equal("leave.approve", Route[1].Permission);
    }

    [Fact]
    public void An_Unknown_Subject_Has_No_Route() =>
        Assert.Empty(ApprovalRoutes.For((ApprovalSubjectType)12345));

    [Fact]
    public void A_Normal_Request_Starts_With_The_Manager_Pending_And_Hr_Waiting()
    {
        var plan = ApprovalRoutePlanner.Plan(Route, Context(Manager), HrHolders(HrUser));

        Assert.Null(plan.Error);
        Assert.Equal(ApprovalStepStatus.Pending, Step(plan, 1).Status);
        Assert.Equal(Manager, Step(plan, 1).ApproverEmployeeId);
        Assert.Equal(ApprovalStepStatus.Waiting, Step(plan, 2).Status);
    }

    [Fact]
    public void No_Manager_Skips_Straight_To_Hr()
    {
        var plan = ApprovalRoutePlanner.Plan(Route, Context(null), HrHolders(HrUser));

        Assert.Null(plan.Error);
        Assert.Equal(ApprovalStepStatus.Skipped, Step(plan, 1).Status);
        Assert.Contains("no reporting manager", Step(plan, 1).SkipReason);
        Assert.Equal(ApprovalStepStatus.Pending, Step(plan, 2).Status);
    }

    [Fact]
    public void A_Manager_Who_Is_The_Requester_Is_Skipped() =>   // the agreed "skip when it would be the requester" rule
        Assert.Contains("your own manager",
            Step(ApprovalRoutePlanner.Plan(Route, Context(Requester), HrHolders(HrUser)), 1).SkipReason);

    [Fact]
    public void A_Manager_Who_Has_Left_Is_Skipped() =>
        Assert.Contains("no longer employed",
            Step(ApprovalRoutePlanner.Plan(Route, Context(Manager, employed: false), HrHolders(HrUser)), 1).SkipReason);

    [Fact]
    public void A_Manager_With_No_Login_Is_Skipped_With_A_Note() =>   // agreed: goes straight to HR
        Assert.Contains("no login linked",
            Step(ApprovalRoutePlanner.Plan(Route, Context(Manager, login: false), HrHolders(HrUser)), 1).SkipReason);

    [Fact]
    public void Skip_Reasons_Are_Checked_In_A_Fixed_Order()
    {
        // not employed AND no login: the "no longer employed" reason wins
        var plan = ApprovalRoutePlanner.Plan(Route, Context(Manager, employed: false, login: false), HrHolders(HrUser));
        Assert.Contains("no longer employed", Step(plan, 1).SkipReason);
    }

    [Fact]
    public void Hr_Step_Is_Skipped_When_Only_The_Requester_Holds_The_Permission()
    {
        var plan = ApprovalRoutePlanner.Plan(Route, Context(Manager), HrHolders(RequesterUser));

        Assert.Null(plan.Error);   // the manager can still approve
        Assert.Equal(ApprovalStepStatus.Pending, Step(plan, 1).Status);
        Assert.Equal(ApprovalStepStatus.Skipped, Step(plan, 2).Status);
    }

    [Fact]
    public void Hr_Step_Is_Skipped_When_Nobody_Holds_The_Permission()
    {
        var plan = ApprovalRoutePlanner.Plan(Route, Context(Manager), HrHolders());
        Assert.Equal(ApprovalStepStatus.Skipped, Step(plan, 2).Status);
    }

    [Fact]
    public void When_Every_Step_Is_Skipped_The_Request_Is_Refused_Not_Auto_Approved()
    {
        var plan = ApprovalRoutePlanner.Plan(Route, Context(null), HrHolders());

        Assert.Equal(ApprovalRoutePlanner.NobodyAvailable, plan.Error);
        Assert.Empty(plan.Steps);
    }

    [Fact]
    public void An_Empty_Route_Is_Refused() =>
        Assert.NotNull(ApprovalRoutePlanner.Plan([], Context(Manager), HrHolders(HrUser)).Error);

    [Fact]
    public void A_Permission_Step_With_No_Permission_Code_Is_Skipped_Rather_Than_Open_To_Everyone()
    {
        var route = new[] { new RouteStepTemplate(ApprovalStepKind.Permission, "Broken", null) };
        Assert.Equal(ApprovalRoutePlanner.NobodyAvailable, ApprovalRoutePlanner.Plan(route, Context(Manager), HrHolders(HrUser)).Error);
    }

    [Fact]
    public void Exactly_One_Step_Is_Pending()
    {
        var plan = ApprovalRoutePlanner.Plan(Route, Context(Manager), HrHolders(HrUser));
        Assert.Equal(1, plan.Steps.Count(s => s.Status == ApprovalStepStatus.Pending));
    }

    [Fact]
    public void Steps_Are_Numbered_From_One_In_Route_Order()
    {
        var plan = ApprovalRoutePlanner.Plan(Route, Context(Manager), HrHolders(HrUser));
        Assert.Equal([1, 2], plan.Steps.Select(s => s.Order).ToArray());
    }
}