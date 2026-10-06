using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class JobOffer : BaseEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }
    
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = default!;
    
    public decimal ProposedSalary { get; set; }
    public string Currency { get; set; } = "PHP";
    public DateOnly ProposedStartDate { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public string? Terms { get; set; }

    public JobOfferStatus Status { get; set; } = JobOfferStatus.Draft;
    public DateTime? SendAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public string? DeclineReason { get; set; }

    public Guid CreatedByUserId { get; set; }
}