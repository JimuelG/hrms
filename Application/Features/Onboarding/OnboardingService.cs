using Core.Common;
using Core.DTOs.Onboarding;
using Core.DTOs.Organization;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Onboarding;

public sealed class OnboardingService(
    IUnitOfWork unit,
    IGenericRepository<OnboardingCase> cases,
    IGenericRepository<OnboardingTaskTemplate> templates,
    IGenericRepository<Core.Entities.Application> applications,
    IGenericRepository<Applicant> applicants,
    IEmployeeService employeeService,
    ITimelineService timeline) : IOnboardingService
{
    public async Task<ServiceResult<OnboardingCaseDto>> CompleteTaskAsync(Guid caseId, Guid taskId, Guid completedByUserId, CancellationToken ct = default)
    {
        var onboardingCase = await cases.GetEntityWithSpec(new OnboardingCaseByIdWithTasksSpecification(caseId), ct);
        if (onboardingCase is null) return ServiceResult<OnboardingCaseDto>.Fail("Onboarding case no found.", ServiceErrorType.NotFound);

        var task = onboardingCase.Tasks.FirstOrDefault(t => t.Id == taskId);
        if (task is null) return ServiceResult<OnboardingCaseDto>.Fail("Onboarding task not found.", ServiceErrorType.NotFound);

        task.IsCompleted = true;
        task.CompletedAtUtc = DateTime.UtcNow;
        task.CompletedByUserId = completedByUserId;

        if (onboardingCase.Tasks.Where(t => t.IsRequired).All(t => t.IsCompleted))
            onboardingCase.Status = OnboardingCaseStatus.ReadyToConvert;

        await unit.Complete();
        return ServiceResult<OnboardingCaseDto>.Success(await ToDtoAsync(onboardingCase, ct));
    }

    public async Task<ServiceResult<EmployeeDto>> ConvertToEmployeeAsync(Guid caseId, ConvertToEmloyeeDto dto, CancellationToken ct = default)
    {
        var onboardingCase = await cases.GetEntityWithSpec(new OnboardingCaseByIdWithTasksSpecification(caseId), ct);
        if (onboardingCase is null) return ServiceResult<EmployeeDto>.Fail("Onboarding case not found.", ServiceErrorType.NotFound);

        if (onboardingCase.Status != OnboardingCaseStatus.ReadyToConvert)
            return ServiceResult<EmployeeDto>.Fail("All required tasks must be completed before converting to an employee.");

        if (onboardingCase.CreatedEmployeeId is not null)
            return ServiceResult<EmployeeDto>.Fail("This onboarding case has already been converted.", ServiceErrorType.Conflict);

        var applicant = await applicants.GetByIdAsync(onboardingCase.Application.ApplicantId, ct);
        if (applicant is null) return ServiceResult<EmployeeDto>.Fail("Applicant record not found.", ServiceErrorType.NotFound);

        var createResult = await employeeService.CreateAsync(new CreateEmployeeDto
        {
            EmployeeNumber = dto.EmployeeNumber,
            FirstName = applicant.FirstName,
            LastName = applicant.LastName,
            Email = applicant.Email,
            Phone = applicant.Phone,
            BranchId = dto.BranchId,
            DepartmentId = dto.DepartmentId,
            PositionId = dto.PositionId,
            ManagerId = dto.ManagerId,
            EmploymentType = (int)dto.EmploymentType,
            HireDate = dto.HireDate,
            Status = (int)EmployeeStatus.Probationary
        }, ct);

        if (!createResult.Succeeded)
            return ServiceResult<EmployeeDto>.Fail(createResult.Error!, createResult.ErrorType);

        onboardingCase.CreatedEmployeeId = createResult.Value!.Id;
        onboardingCase.Status = OnboardingCaseStatus.Completed;
        onboardingCase.CompletedAtUtc = DateTime.UtcNow;

        timeline.Record(
            TimelineSubjectType.Applicant,
            onboardingCase.Application.ApplicantId,
            TimeLineEventType.Note,
            "Converted to employee", $"Employee number {dto.EmployeeNumber}.");

        await unit.Complete();
        return ServiceResult<EmployeeDto>.Success(createResult.Value);
    }

    public async Task<OnboardingCaseDto?> GetByApplicationAsync(Guid applicationId, CancellationToken ct = default)
    {
        var onboardingCase = await cases.GetEntityWithSpec(new OnboardingCaseByApplicationSpecification(applicationId), ct);
        return onboardingCase is null ? null : await ToDtoAsync(onboardingCase, ct);
    }

    public async Task<OnboardingCaseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var onboardingCase = await cases.GetEntityWithSpec(new OnboardingCaseByIdWithTasksSpecification(id), ct);
        return onboardingCase is null ? null : await ToDtoAsync(onboardingCase, ct);
    }

    public async Task<ServiceResult<OnboardingCaseDto>> ReopenTaskAsync(Guid caseId, Guid taskId, CancellationToken ct = default)
    {
        var onboardingCase = await cases.GetEntityWithSpec(new OnboardingCaseByIdWithTasksSpecification(caseId), ct);
        if (onboardingCase is null) return ServiceResult<OnboardingCaseDto>.Fail("Onboarding case not found.", ServiceErrorType.NotFound);

        var task = onboardingCase.Tasks.FirstOrDefault(t => t.Id == taskId);
        if (task is null) return ServiceResult<OnboardingCaseDto>.Fail("Onboarding task not found.", ServiceErrorType.NotFound);

        task.IsCompleted = false;
        task.CompletedAtUtc = null;
        task.CompletedByUserId = null;

        if (onboardingCase.Status == OnboardingCaseStatus.ReadyToConvert)
            onboardingCase.Status = OnboardingCaseStatus.InProgress;

        await unit.Complete();
        return ServiceResult<OnboardingCaseDto>.Success(await ToDtoAsync(onboardingCase, ct));
    }

    public async Task<ServiceResult<OnboardingCaseDto>> StartAsync(StartOnboardingDto dto, CancellationToken ct = default)
    {
        var application = await applications.GetByIdAsync(dto.ApplicationId, ct);
        if (application is null) return ServiceResult<OnboardingCaseDto>.Fail("Application not found.", ServiceErrorType.NotFound);

        if (application.Status != ApplicationStatus.Hired)
            return ServiceResult<OnboardingCaseDto>.Fail("Onboarding can only start for an application marked Hired.");

        var existing = await cases.GetEntityWithSpec(new OnboardingCaseByApplicationSpecification(dto.ApplicationId), ct);
        if (existing is not null)
            return ServiceResult<OnboardingCaseDto>.Fail("Onboarding has already been started for this application.", ServiceErrorType.Conflict);

        var activeTemplates = await templates.ListAsync(new ActiveTaskTemplatesSpecification(), ct);

        var onboardingCase = new OnboardingCase { ApplicationId = dto.ApplicationId };
        cases.Add(onboardingCase);

        foreach (var t in activeTemplates)
        {
            onboardingCase.Tasks.Add(new OnboardingTask
            {
               Title = t.Title,
               Description = t.Description,
               IsRequired = t.IsRequired,
               SortOrder = t.SortOrder
            });
        }

        timeline.Record(
            TimelineSubjectType.Applicant,
            application.ApplicantId,
            TimeLineEventType.Note,
            "Onboarding started");

        await unit.Complete();

        var saved = await cases.GetEntityWithSpec(new OnboardingCaseByIdWithTasksSpecification(onboardingCase.Id), ct);
        return ServiceResult<OnboardingCaseDto>.Success(await ToDtoAsync(saved!, ct));
    }

    private async Task<OnboardingCaseDto> ToDtoAsync(OnboardingCase c, CancellationToken ct)
    {
        var applicant = await applicants.GetByIdAsync(c.Application.ApplicantId, ct);
        var application = await applications.GetEntityWithSpec(new ApplicationByIdWithRelationsSpecification(c.ApplicationId), ct);


        return new OnboardingCaseDto(
            c.Id,
            c.ApplicationId,
            applicant is null ? "" : $"{applicant.FirstName} {applicant.LastName}",
            application?.JobPosting.Title ?? "",
            (int)c.Status,
            c.StartedAtUtc,
            c.CompletedAtUtc,
            c.CreatedEmployeeId,
            c.Tasks.OrderBy(t => t.SortOrder)
                .Select(t => new OnboardingTaskDto(t.Id, t.Title, t.Description, t.IsRequired, t.IsCompleted, t.CompletedAtUtc))
                .ToList());
    }
}