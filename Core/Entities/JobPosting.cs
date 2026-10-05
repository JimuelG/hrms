using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class JobPosting : SoftDeletableEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }
    public string Title { get; set; } = default!;
    public Guid PositionId { get; set; }
    public Position Position { get; set; } = default!;
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = default!;
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = default!;


    public EmploymentType EmploymentType { get; set; }
    
    public string Description { get; set; } = default!;
    public string? Requirements { get; set; }
    public string? Skills { get; set; }
    public string? EducationRequirement { get; set; }
    public string? ExperienceRequirement { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public bool SalaryVisible { get; set; }

    public int Vacancies { get; set; } = 1;
    public DateOnly? ApplicationDeadline { get; set; }

    public Guid? HiringManagerId { get; set; }
    public Employee? HiringManager { get; set; }

    public JobPostingStatus Status { get; set; } = JobPostingStatus.Draft;

}