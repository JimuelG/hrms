using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class LeaveRequest : BaseEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }

    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DayPortion StartPortion { get; set; } = DayPortion.Full;
    public DayPortion EndPortion { get; set; } = DayPortion.Full;

    public decimal Days { get; set; }
    public int LeaveYear { get; set; }
    public bool IsPaid { get; set; }
    public string Reason { get; set; } = default!;

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public DateTime? DecidedAtUtc { get; set; }
    
    public DateTime? CancelledAtUtc { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public string? CancellationReason { get; set; }
}