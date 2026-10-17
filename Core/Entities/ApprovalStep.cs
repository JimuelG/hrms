using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class ApprovalStep : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public Guid ApprovalRequestId { get; set; }
    public ApprovalRequest Request { get; set; } = default!;

    public int Order { get; set; }
    public ApprovalStepKind Kind { get; set; }
    public string Label { get; set; } = default!;

    public string? Permission { get; set; }

    public Guid? ApproverEmployeeId { get; set; }

    public ApprovalStepStatus Status { get; set; }
    public string? SkipReason { get; set; }

    public Guid? DecidedByUserId { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public string? Note { get; set; }
}