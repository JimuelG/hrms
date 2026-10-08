using System.Reflection.Metadata;
using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class OnboardingTaskTemplate : SoftDeletableEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsRequired { get; set; } = true;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class OnboardingCase: BaseEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }

    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = default!;
    public OnboardingCaseStatus Status { get; set; } = OnboardingCaseStatus.InProgress;
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public Guid? CreatedEmployeeId { get; set; }
    public ICollection<OnboardingTask> Tasks { get; set; } = [];
}

public class OnboardingTask : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OnboardingCaseId { get; set; }
    public OnboardingCase OnboardingCase { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public Guid? CompletedByUserId { get; set; }
}