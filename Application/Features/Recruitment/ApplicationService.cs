using Core.Common;
using Core.DTOs.Recruitment;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Recruitment;

public class ApplicationService(
    IUnitOfWork unit,
    IGenericRepository<Core.Entities.Application> applications,
    IGenericRepository<Applicant> applicants,
    IGenericRepository<JobPosting> postings)
        : IApplicationService
{
    public async Task<ServiceResult<ApplicationDto>> CreateAsync(CreateApplicationDto dto, CancellationToken ct = default)
    {
        if (await applicants.GetByIdAsync(dto.ApplicantId, ct) is null)
            return ServiceResult<ApplicationDto>.Fail("Applicant not found.", ServiceErrorType.NotFound);
        if (await postings.GetByIdAsync(dto.JobPostingId, ct) is null)
            return ServiceResult<ApplicationDto>.Fail("Job posting not found.", ServiceErrorType.NotFound);
        
        if (await applications.CountAsync(new DuplicateApplicationSpecification(dto.ApplicantId, dto.JobPostingId), ct) > 0)
            return ServiceResult<ApplicationDto>.Fail("This applicant has already applied to this job posting", ServiceErrorType.Conflict);
        
        var application = new Core.Entities.Application
        {
            ApplicantId = dto.ApplicantId,
            JobPostingId = dto.JobPostingId,
            Notes = dto.Notes
        };

        applications.Add(application);
        await unit.Complete();

        var saved = await applications.GetEntityWithSpec(new ApplicationByIdWithRelationsSpecification(application.Id), ct);
        return ServiceResult<ApplicationDto>.Success(ToDto(saved!));
    }

    public async Task<ServiceResult<ApplicationDto>> UpdateStatusAsync(Guid id, UpdateApplicationStatusDto dto, CancellationToken ct = default)
    {
        var application = await applications.GetByIdAsync(id, ct);
        if (application is null) return ServiceResult<ApplicationDto>.Fail("Application not found.", ServiceErrorType.NotFound);

        application.Status = (ApplicationStatus)dto.Status;
        if (dto.Notes is not null) application.Notes = dto.Notes;

        await unit.Complete();
        var saved = await applications.GetEntityWithSpec(new ApplicationByIdWithRelationsSpecification(id), ct);
        return ServiceResult<ApplicationDto>.Success(ToDto(saved!));
    }

    public async Task<IReadOnlyList<ApplicationDto>> GetByPostingAsync(Guid jobPostingId, ApplicationStatus? status, CancellationToken ct = default) =>
        (await applications.ListAsync(new ApplicationsByPostingSpecification(jobPostingId, status), ct)).Select(ToDto).ToList();

    public async Task<IReadOnlyList<ApplicationDto>> GetByApplicantAsync(Guid applicantId, CancellationToken ct = default) =>
        (await applications.ListAsync(new ApplicationsByApplicantSpecification(applicantId), ct)).Select(ToDto).ToList();

    public async Task<ApplicationDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var application = await applications.GetEntityWithSpec(new ApplicationByIdWithRelationsSpecification(id), ct);
        return application is null ? null : ToDto(application);
    }

    private static ApplicationDto ToDto(Core.Entities.Application a) => new(
        a.Id,
        a.ApplicantId,
        $"{a.Applicant.FirstName} {a.Applicant.LastName}",
        a.Applicant.Email,
        a.JobPostingId,
        a.JobPosting.Title,
        (int)a.Status,
        a.Notes,
        a.AppliedAtUtc);

}