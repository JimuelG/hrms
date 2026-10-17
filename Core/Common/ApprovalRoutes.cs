using Core.Enums;

namespace Core.Common;
public static class ApprovalRoutes
{
    public const string LeaveHrPermission = "leave.approve";

    public static IReadOnlyList<RouteStepTemplate> For(ApprovalSubjectType subject) => subject switch
    {
        ApprovalSubjectType.LeaveRequest =>
        [
            new(ApprovalStepKind.Manager, "Manager approval", null),
            new(ApprovalStepKind.Permission, "HR approval", LeaveHrPermission)
        ],
        _ => []
    };
}