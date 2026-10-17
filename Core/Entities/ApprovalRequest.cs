using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class ApprovalRequest : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    public ApprovalSubjectType SubjectType { get; set; }
    public Guid SubjectId { get; set; }

    public Guid RequesterEmployeeId { get; set; }
    public string Title { get; set; } = default!;
    public string? Details { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public DateTime? CompletedAtUtc { get; set; }

    public DateTime LastActivityAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = default!;

    public ICollection<ApprovalStep> Steps { get; set; } = [];
    public ICollection<ApprovalEvent> Events { get; set; } = [];
}