using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class ApprovalEvent : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid ApprovalRequestId { get; set; }
    public ApprovalRequest Request { get; set; } = default!;

    public ApprovalEventType Type { get; set; }
    public int? StepOrder { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? Note { get; set; }
}