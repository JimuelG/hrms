using Core.Entities;

namespace Core.Specifications;
public sealed class ApplicationsInWindowSpecification : BaseSpecfication<Application>
{
    public ApplicationsInWindowSpecification(DateTime fromUtc, DateTime toExclusiveUtc, Guid? postingId, Guid? departmentId)
        : base(a => a.AppliedAtUtc >= fromUtc && a.AppliedAtUtc < toExclusiveUtc
                && (postingId == null || a.JobPostingId == postingId)
                && (departmentId == null || a.JobPostingId == postingId)
                && (departmentId == null || a.JobPosting.DepartmentId == departmentId))
    {
        AddInclude(a => a.JobPosting);
    }
}

public sealed class InterviewsForApplicationsSpecification : BaseSpecfication<Interview>
{
    public InterviewsForApplicationsSpecification(List<Guid> applicationIds) : base(i => applicationIds.Contains(i.ApplicationId))
    {
        
    }
}

public sealed class OffersForApplicationsSpecification : BaseSpecfication<JobOffer>
{
    public OffersForApplicationsSpecification(List<Guid> applicationIds) : base(o => applicationIds.Contains(o.ApplicationId))
    {
        
    }
}