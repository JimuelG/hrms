using Core.Common;
using Core.DTOs.Platform;

namespace Core.Interfaces;
public interface IPlatformSubscriptionService
{
    Task<IReadOnlyList<SubscriptionPlanDto>> GetPlansAsync(CancellationToken ct = default);
    Task<ServiceResult<TenantSubscriptionDto>> AssignPlanAsync(Guid tenantId, Guid planId, CancellationToken ct = default);
}