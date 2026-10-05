using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class InterviewEvaluation : BaseEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }

    public Guid InterviewId { get; set; }
    public Interview Interview { get; set; } = default!;

    [System.ComponentModel.DataAnnotations.Range(1, 5)]
    public int Rating { get; set; }
    public InterviewRecommendation Recommendation { get; set; }
    public string? Strenghts { get; set; }
    public string? Concerns { get; set; }
    public string? Notes { get; set; }

    public Guid SubmittedByUserId { get; set; }
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
}