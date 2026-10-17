using Core.Enums;

namespace Core.Common;
public static class ApprovalRoutePlanner
{
    public const string NobodyAvailable = "Nobody is available to approve this request. Contact HR.";

    public static RoutePlan Plan(IReadOnlyList<RouteStepTemplate> route, RequesterContext requester, PermissionHolders holders)
    {
        if (route.Count == 0) return RoutePlan.Fail("No approval route is defined for this kind of request.");

        var steps = new List<PlannedStep>();
        var pendingAssigned = false;

        for (var i = 0; i < route.Count; i++)
        {
            var template = route[i];
            var order = i + 1;

            var (approver, skipReason) = Resolve(template, requester, holders);

            ApprovalStepStatus status;
            if (skipReason is not null) status = ApprovalStepStatus.Skipped;
            else if (!pendingAssigned) { status = ApprovalStepStatus.Pending; pendingAssigned = true; }
            else status = ApprovalStepStatus.Waiting;

            steps.Add(new PlannedStep(order, template.Kind, template.Label, template.Permission, approver, status, skipReason));
        }

        return pendingAssigned
            ? new RoutePlan(steps, null)
            : RoutePlan.Fail(NobodyAvailable);
    }

    private static (Guid? Approver, string? SkipReason) Resolve(
        RouteStepTemplate step, RequesterContext requester, PermissionHolders holders)
    {
        if (step.Kind == ApprovalStepKind.Manager)
        {
            if (requester.ManagerEmployeeId is not Guid manager)
                return (null, "You have no reporting manager.");
            if (manager == requester.EmployeeId)
                return (null, "You are your own manager.");
            if (!requester.ManagerIsEmployed)
                return (null, "Your reporting manager is no longer employed.");
            if (!requester.ManagerHasLogin)
                return(null, "Your reporting manager has no login linked, so this step was skipped.");
            return (manager, null);
        }

        if (string.IsNullOrWhiteSpace(step.Permission) || !holders.HasAnyOneBesides(step.Permission, requester.UserId))
            return (null, "Nobody else holds the permission for this step.");

        return (null, null);
    }
}