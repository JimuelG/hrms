using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class AttendanceCorrectionRequest : BaseEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;

    public DateOnly Date { get; set; }
    public TimeOnly? RequestedClockIn { get; set; }
    public TimeOnly? RequestedClockOut { get; set; }
    public string Reason { get; set; } = default!;

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public Guid RequestedByUserId { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewNote { get; set; }
}