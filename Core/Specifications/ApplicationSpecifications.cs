using Core.Entities;
using Core.Enums;

namespace Core.Specifications;
public sealed class ApplicationsByPostingSpecification : BaseSpecfication<Application>
{
    public ApplicationsByPostingSpecification(Guid jobPostingId, ApplicationStatus? status = null)
        : base(a => a.JobPostingId == jobPostingId && (status == null || a.Status == status))
    {
        AddInclude(a => a.Applicant);
        AddInclude(a => a.JobPosting);
        AddOrderByDescending(a => a.AppliedAtUtc);   
    }
}

public sealed class ApplicationsByApplicantSpecification : BaseSpecfication<Application>
{
    public ApplicationsByApplicantSpecification(Guid applicantId)
        : base(a => a.ApplicantId == applicantId)
    {
        AddInclude(a => a.Applicant);
        AddInclude(a => a.JobPosting);
        AddOrderByDescending(a => a.AppliedAtUtc);
    }
}

public sealed class DuplicateApplicationSpecification : BaseSpecfication<Application>
{
    public DuplicateApplicationSpecification(Guid applicantId, Guid jobPostingId)
        : base(a => a.ApplicantId == applicantId && a.JobPostingId == jobPostingId) {}
}

public sealed class ApplicationByIdWithRelationsSpecification : BaseSpecfication<Application>
{
    public ApplicationByIdWithRelationsSpecification(Guid id) : base(a => a.Id == id)
    {
        AddInclude(a => a.Applicant);
        AddInclude(a => a.JobPosting);
    }
}