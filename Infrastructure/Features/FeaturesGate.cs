using Core.Common;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Features;

public sealed class FeaturesGate(
    AppDbContext db,
    ITenantContext tenant) : IFeatureGate
{
    public async Task EnsureFeatureEnabledAsync(PlanFeature feature, CancellationToken ct = default)
    {
        var sub = await LoadSubscriptionAsync(ct);

        if (sub.Status is TenantSubscriptionStatus.Suspended or TenantSubscriptionStatus.Cancelled or TenantSubscriptionStatus.Expired)
            throw new FeatureGateException($"Your subscription is {sub.Status}. Contact your administrator.");
        
        if (!sub.Plan.Features.HasFlag(feature))
            throw new FeatureGateException($"Your current plan ({sub.Plan.Name}) does not include this feature.");
    }

    public async Task EnsureWithinLimitAsync(UsageType type, int currentCount, CancellationToken ct = default)
    {
        var sub = await LoadSubscriptionAsync(ct);

        var limit = type switch
        {
            UsageType.Branches => sub.Plan.MaxBranches,
            UsageType.Departments => sub.Plan.MaxDepartments,
            UsageType.Employees => sub.Plan.MaxEmployees,
            UsageType.HrUsers => sub.Plan.MaxHrUsers,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        if (limit < 0) return;

        if (currentCount + 1 > limit)
            throw new FeatureGateException(
                $"Your plan ({sub.Plan.Name}) allows up to {limit} {type.ToString().ToLowerInvariant()}. Upgade to add more.");
    }

    private async Task<TenantSubscription> LoadSubscriptionAsync(CancellationToken ct)
    {
        var sub = await db.TenantSubscriptions.Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.TenantId == tenant.RequiredTenantId, ct);

        if (sub is null)
            throw new FeatureGateException("No active subscription found for this tenant.");

        return sub;
    }
}