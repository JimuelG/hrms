using Core.Entities;
using Core.Enums;

namespace Core.DTOs.Platform;
public sealed record SubscriptionPlanDto(
    Guid Id,
    string Code,
    string Name,
    int MaxEmployees,
    int MaxBranches,
    int MaxDepartments,
    int MaxHrUsers,
    PlanFeature Features);

public sealed record TenantSubscriptionDto(
    SubscriptionPlanDto Plan,
    TenantSubscriptionStatus status,
    DateTime? TrialEndsAtUtc,
    DateTime? CurrentPeriodEndsAtUtc);

public sealed class AssignPlanDto
{
    public Guid SubscriptionPlanId { get; init; }
}