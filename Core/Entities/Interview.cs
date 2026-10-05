using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class Interview : BaseEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }
    
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = default!;

    public InterviewType Type { get; set; }
    public DateTime ScheduledAtUtc { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public string? Location { get; set; }

    public Guid InterviewerId { get; set; }
    public Employee Interviewer { get; set; } = default!;

    public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;
    public string? CancellationReason { get; set; }

    public InterviewEvaluation? Evaluation { get; set; }
}