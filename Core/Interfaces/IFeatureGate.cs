using Core.Enums;

namespace Core.Interfaces;

public enum UsageType
{
    Branches,
    Departments,
    Employees,
    HrUsers
}
public interface IFeatureGate
{
    Task EnsureFeatureEnabledAsync(PlanFeature feature, CancellationToken ct = default);
    Task EnsureWithinLimitAsync(UsageType type, int currentCount, CancellationToken ct = default);
}