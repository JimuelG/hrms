using Core.Common;
using Core.DTOs.Recruitment;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Recruitment;

public class JobPostingService(
    IUnitOfWork unit,
    IGenericRepository<JobPosting> postings,
    IGenericRepository<Position> positions,
    IGenericRepository<Department> departments,
    IGenericRepository<Branch> branches,
    IGenericRepository<Employee> employees,
    IFeatureGate featureGate) : IJobPostingService
{
    public async Task<ServiceResult<JobPostingDto>> CreateAsync(CreateJobPostingDto dto, CancellationToken ct = default)
    {
        await featureGate.EnsureFeatureEnabledAsync(PlanFeature.Ats, ct);

        var fkError = await ValidateForeignKeysAsync(dto.PositionId, dto.DepartmentId, dto.BranchId, dto.HiringManagerId, ct);
        if (fkError is not null) return ServiceResult<JobPostingDto>.Fail(fkError);

        var posting = new JobPosting
        {
            Title = dto.Title,
            PositionId = dto.PositionId,
            DepartmentId = dto.DepartmentId,
            BranchId = dto.BranchId,
            EmploymentType = (EmploymentType)dto.EmploymentType,
            Description = dto.Description,
            Requirements = dto.Requirements,
            Skills = dto.Skills,
            EducationRequirement = dto.EducationRequirement,
            ExperienceRequirement = dto.ExperienceRequirement,
            SalaryMin = dto.SalaryMin,
            SalaryMax = dto.SalaryMax,
            SalaryVisible = dto.SalaryVisible,
            Vacancies = dto.Vacancies,
            ApplicationDeadline = dto.ApplicationDeadline,
            HiringManagerId = dto.HiringManagerId
        };

        postings.Add(posting);
        await unit.Complete();

        var saved = await postings.GetEntityWithSpec(new JobPostingWithRelationsByIdSpecification(posting.Id), ct);
        return ServiceResult<JobPostingDto>.Success(ToDto(saved!));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var posting = await postings.GetByIdAsync(id, ct);
        if (posting is null) return ServiceResult<bool>.Fail("Job posting not found.", ServiceErrorType.NotFound);

        postings.Remove(posting);
        await unit.Complete();
        return ServiceResult<bool>.Success(true);
    }

    public async Task<IReadOnlyList<JobPostingDto>> GetAllAsync(string? search, JobPostingStatus? status, CancellationToken ct = default) =>
        (await postings.ListAsync(new JobPostingSearchSpecification(search, status), ct)).Select(ToDto).ToList();

    public async Task<JobPostingDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var posting = await postings.GetEntityWithSpec(new JobPostingWithRelationsByIdSpecification(id), ct);
        return posting is null ? null : ToDto(posting);
    }

    public async Task<ServiceResult<JobPostingDto>> UpdateAsync(Guid id, UpdateJobPostingDto dto, CancellationToken ct = default)
    {
        var posting = await postings.GetByIdAsync(id, ct);
        if (posting is null) return ServiceResult<JobPostingDto>.Fail("Job posting not found", ServiceErrorType.NotFound);

        var fkError = await ValidateForeignKeysAsync(dto.PositionId, dto.DepartmentId, dto.BranchId, dto.HiringManagerId, ct);
        if (fkError is not null) return ServiceResult<JobPostingDto>.Fail(fkError);

        posting.Title = dto.Title;
        posting.PositionId = dto.PositionId;
        posting.DepartmentId = dto.DepartmentId;
        posting.BranchId = dto.BranchId;
        posting.EmploymentType = (EmploymentType)dto.EmploymentType;
        posting.Description = dto.Description;
        posting.Requirements = dto.Requirements;
        posting.Skills = dto.Skills;
        posting.EducationRequirement = dto.EducationRequirement;
        posting.ExperienceRequirement = dto.ExperienceRequirement;
        posting.SalaryMin = dto.SalaryMin;
        posting.SalaryMax = dto.SalaryMax;
        posting.SalaryVisible = dto.SalaryVisible;
        posting.Vacancies = dto.Vacancies;
        posting.ApplicationDeadline = dto.ApplicationDeadline;
        posting.HiringManagerId = dto.HiringManagerId;
        posting.Status = (JobPostingStatus)dto.Status;

        await unit.Complete();
        var saved = await postings.GetEntityWithSpec(new JobPostingWithRelationsByIdSpecification(id), ct);
        return ServiceResult<JobPostingDto>.Success(ToDto(saved!));
    }

    private async Task<string?> ValidateForeignKeysAsync(Guid positionId, Guid departmentId, Guid branchId, Guid? hiringManagerId, CancellationToken ct)
    {
        if (await positions.GetByIdAsync(positionId, ct) is null)
            return "Selected position was not found."; 
        if (await departments.GetByIdAsync(departmentId, ct) is null)
            return "Selected department was not found.";
        if (await branches.GetByIdAsync(branchId, ct) is null)
            return "Selected branch was not found.";
        if (hiringManagerId is Guid mid && employees.GetByIdAsync(mid, ct) is null)
            return "Selected hiring manager was not found";
        return null;
    }

    private static JobPostingDto ToDto(JobPosting j) => new(
        j.Id,
        j.Title,
        j.PositionId,
        j.Position.Title,
        j.DepartmentId,
        j.Department.Name,
        j.BranchId,
        j.Branch.Name,
        (int)j.EmploymentType,
        j.Description,
        j.Requirements,
        j.Skills,
        j.EducationRequirement,
        j.ExperienceRequirement,
        j.SalaryMin,
        j.SalaryMax,
        j.SalaryVisible,
        j.Vacancies,
        j.ApplicationDeadline,
        j.HiringManagerId,
        j.HiringManager is null ? null : $"{j.HiringManager.FirstName} {j.HiringManager.LastName}",
        (int)j.Status,
        j.CreatedAtUtc);
}