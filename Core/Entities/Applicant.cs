using Core.Interfaces;

namespace Core.Entities;
public class Applicant : SoftDeletableEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }

    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Phone { get; set; }

    public string? SkillsSumarry { get; set; }
    public string? EducationSummary { get; set; }
    public string? ExperienceSummary { get; set; }

    public string? ResumeOriginalFileName { get; set; }
    public string? ResumeContentType { get; set; }
    public long? ResumeSizeBytes { get; set; }
    public string?  ResumeStorageKey { get; set; }

    public ICollection<Application> Applications { get; set; } = [];
}