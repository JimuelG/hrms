using Core.Entities;
using Core.Enums;

namespace Core.Specifications;
public sealed class JobOfferByApplicationSpecification : BaseSpecfication<JobOffer>
{
    public JobOfferByApplicationSpecification(Guid applicationId) : base(o => o.ApplicationId == applicationId)
    {
        AddOrderByDescending(o => o.CreatedAtUtc);
    }
}

public sealed class ActiveJobOfferByApplicationSpecification : BaseSpecfication<JobOffer>
{
    private static readonly JobOfferStatus[] ActiveStatuses = [JobOfferStatus.Draft, JobOfferStatus.Sent];

    public ActiveJobOfferByApplicationSpecification(Guid applicationId) 
        : base(o => o.ApplicationId == applicationId && ActiveStatuses.Contains(o.Status))
    {
        
    }
}
