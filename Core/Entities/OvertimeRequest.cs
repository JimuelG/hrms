using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class OvertimeRequest : BaseEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;

    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int RequestedMinutes { get; set; }
    public int? ApprovedMinutes { get; set; }
    public string Reason { get; set; } = default!;

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewNote { get; set; }
}