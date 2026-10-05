using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class TimelineEvent : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    
    public TimelineSubjectType SubjectType { get; set; }
    public Guid SubjectId { get; set; }

    public TimeLineEventType EventType { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime EventDateUtc { get; set; } = DateTime.UtcNow;
    public Guid? RecordedByUserId { get; set; }
}