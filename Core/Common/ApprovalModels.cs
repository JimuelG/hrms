using Core.Enums;

namespace Core.Common;

public sealed record RouteStepTemplate(ApprovalStepKind Kind, string Label, string? Permission);

public sealed record RequesterContext(
    Guid EmployeeId,
    Guid? UserId,
    Guid? ManagerEmployeeId,
    bool ManagerIsEmployed,
    bool ManagerHasLogin);

public sealed record PermissionHolders(IReadOnlyDictionary<string, IReadOnlySet<Guid>> UserIdsByPermission)
{
    public bool HasAnyOneBesides(string permission, Guid? excludedUserId) =>
        UserIdsByPermission.TryGetValue(permission, out var ids) && ids.Any(ids => ids != excludedUserId);
}

public sealed record StepState(
    int Order,
    ApprovalStepKind Kind,
    string Label,
    string? Permission,
    Guid? ApproverEmployeeId,
    ApprovalStepStatus Status,
    string? SkipReason);

public sealed record PlannedStep(
    int Order,
    ApprovalStepKind Kind,
    string Label,
    string? Permission,
    Guid? ApproverEmployeeId,
    ApprovalStepStatus Status,
    string? SkipReason);

public sealed record RoutePlan(IReadOnlyList<PlannedStep> Steps, string? Error)
{
    public static RoutePlan Fail(string error) => new([], error);
}

public sealed record StepOutcome(
    IReadOnlyList<StepState> Steps,
    ApprovalStatus RequestStatus,
    int? ActiveStepOrder,
    bool IsFinal,
    string? Error)
{
    public static StepOutcome Fail(IReadOnlyList<StepState> steps, ApprovalStatus status, string error) =>
        new(steps, status, null, status != ApprovalStatus.Pending, error);
}

public sealed record ApprovalAccess(bool IsRequester, bool IsStepApprover, bool CanSeeRequest, bool CanDecideNow);

