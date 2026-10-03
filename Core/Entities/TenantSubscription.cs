using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class TenantSubscription : BaseEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public SubscriptionPlan Plan { get; set; } = default!;
    
    public TenantSubscriptionStatus Status { get; set; } = TenantSubscriptionStatus.Trial;
    public DateTime? TrialEndsAtUtc { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CurrentPeriodEndsAtUtc { get; set; }
}