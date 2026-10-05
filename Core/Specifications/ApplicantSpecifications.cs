using Core.Entities;

namespace Core.Specifications;
public sealed class ApplicantSearchSpecification : BaseSpecfication<Applicant>
{
    public ApplicantSearchSpecification(string? search) 
        : base(a => string.IsNullOrEmpty(search)
            || a.FirstName.Contains(search)
            || a.LastName.Contains(search)
            || a.Email.Contains(search))
    {
        AddOrderByDescending(a => a.CreatedAtUtc);
    }
}

public sealed class ApplicantByEmailSpecification : BaseSpecfication<Applicant>
{
    public ApplicantByEmailSpecification(string email, Guid? excludeId = null)
        : base(a => a.Email == email && (excludeId == null || a.Id != excludeId)) {}
}