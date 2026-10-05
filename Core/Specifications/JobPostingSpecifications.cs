using Core.Entities;
using Core.Enums;

namespace Core.Specifications;
public sealed class JobPostingSearchSpecification : BaseSpecfication<JobPosting>
{
    public JobPostingSearchSpecification(string? search, JobPostingStatus? status)
        : base(j => (string.IsNullOrEmpty(search) || j.Title.Contains(search)) && (status == null || j.Status == status))
    {
        AddInclude(j => j.Position);
        AddInclude(j => j.Department);
        AddInclude(j => j.Branch);
        AddInclude(j => j.HiringManager!);
        AddOrderByDescending(j => j.CreatedAtUtc);
    }
}

public sealed class JobPostingWithRelationsByIdSpecification : BaseSpecfication<JobPosting>
{
    public JobPostingWithRelationsByIdSpecification(Guid id) : base(j => j.Id == id)
    {
        AddInclude(j => j.Position);
        AddInclude(j => j.Department);
        AddInclude(j => j.Branch);
        AddInclude(j => j.HiringManager!);
    }
}