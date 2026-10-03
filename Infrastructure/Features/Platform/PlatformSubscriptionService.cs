using Core.Common;
using Core.DTOs.Platform;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Features.Platform;
public class PlatformSubscriptionService(
    AppDbContext db,
    ITenantContextInitializer tenantInit)
    : IPlatformSubscriptionService
{
    public async Task<ServiceResult<TenantSubscriptionDto>> AssignPlanAsync(Guid tenantId, Guid planId, CancellationToken ct = default)
    {
        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return ServiceResult<TenantSubscriptionDto>.Fail("Plan not found.", ServiceErrorType.NotFound);

        tenantInit.SetTenant(tenantId);

        var sub =await db.TenantSubscriptions.Include(s => s.Plan).FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);
        if (sub is null)
        {
            sub = new TenantSubscription
            {
                TenantId = tenantId,
                SubscriptionPlanId = planId,
                Status = TenantSubscriptionStatus.Active
            };
            db.TenantSubscriptions.Add(sub);
        }
        else
        {
            sub.SubscriptionPlanId = planId;
            sub.Plan = plan;
        }

        await db.SaveChangesAsync(ct);
        return ServiceResult<TenantSubscriptionDto>.Success(
            new TenantSubscriptionDto(ToDto(plan), sub.Status, sub.TrialEndsAtUtc, sub.CurrentPeriodEndsAtUtc));
    }

    public async Task<IReadOnlyList<SubscriptionPlanDto>> GetPlansAsync(CancellationToken ct = default) =>
        (await db.SubscriptionPlans.Where(p => p.IsActive).ToListAsync(ct)).Select(ToDto).ToList();

    private static SubscriptionPlanDto ToDto(SubscriptionPlan p) => 
        new(
            p.Id,
            p.Code,
            p.Name,
            p.MaxEmployees,
            p.MaxBranches,
            p.MaxDepartments,
            p.MaxHrUsers,
            p.Features);
}