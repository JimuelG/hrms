using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class Application : BaseEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }

    public Guid ApplicantId { get; set; }
    public Applicant Applicant { get; set; } = default!;

    public Guid JobPostingId { get; set; }
    public JobPosting JobPosting { get; set; } = default!;
    
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;
    public string? Notes { get; set; }
    public DateTime AppliedAtUtc { get; set; } = DateTime.UtcNow;
}