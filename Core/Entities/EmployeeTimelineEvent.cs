using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class EmployeeTimelineEvent : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;

    public TimeLineEventType EventType { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime EventDateUtc { get; set; } = DateTime.UtcNow;
    public Guid? RecordedByUserId { get; set; }
}