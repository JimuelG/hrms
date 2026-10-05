using Core.Common;
using Core.DTOs.Recruitment;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Recruitment;

public class InterviewService(
    IUnitOfWork unit,
    IGenericRepository<Interview> interviews,
    IGenericRepository<InterviewEvaluation> evaluations,
    IGenericRepository<Core.Entities.Application> applications,
    IGenericRepository<Employee> employees,
    ITimelineService timeline) : IInterviewService
{
    public async Task<ServiceResult<InterviewDto>> CancelAsync(Guid id, CancelInterviewDto dto, CancellationToken ct = default)
    {
        var interview = await interviews.GetByIdAsync(id, ct);
        if (interview is null) return ServiceResult<InterviewDto>.Fail("Interview not found.", ServiceErrorType.NotFound);

        if (interview.Status != InterviewStatus.Scheduled)
            return ServiceResult<InterviewDto>.Fail("Only a Scheduled interview can be cancelled.");

        interview.Status = InterviewStatus.Cancelled;
        interview.CancellationReason = dto.CancellationReason;

        await unit.Complete();
        var saved = await interviews.GetEntityWithSpec(new InterviewByIdWithRelationsSpecification(id), ct);
        return ServiceResult<InterviewDto>.Success(await ToDtoAsync(saved!, applications, ct));
    }

    public async Task<ServiceResult<InterviewDto>> CompleteAsync(Guid id, CancellationToken ct = default)
    {
        var interview = await interviews.GetByIdAsync(id, ct);
        if (interview is null) return ServiceResult<InterviewDto>.Fail("Interview not found.", ServiceErrorType.NotFound);

        if (interview.Status != InterviewStatus.Scheduled)
            return ServiceResult<InterviewDto>.Fail("Only a Scheduled interview can be marked Completed.");

        interview.Status = InterviewStatus.Completed;
        await unit.Complete();

        timeline.Record(
            TimelineSubjectType.Applicant, 
            interview.ApplicationId, 
            TimeLineEventType.Note, 
            $"{interview.Type} interview completed");

        var saved = await interviews.GetEntityWithSpec(new InterviewByIdWithRelationsSpecification(id), ct);
        return ServiceResult<InterviewDto>.Success(await ToDtoAsync(saved!, applications, ct));
    }

    public async Task<IReadOnlyList<InterviewDto>> GetByApplicationAsync(Guid applicationId, CancellationToken ct = default)
    {
        var results = await interviews.ListAsync(new InterviewsByApplicationSpecification(applicationId), ct);
        var dtos = new List<InterviewDto>();

        foreach (var i in results)
        {
            dtos.Add(await ToDtoAsync(i, applications, ct));
        }
        return dtos;
    }

    public async Task<IReadOnlyList<InterviewDto>> GetByInterviewerAsync(Guid interviewerId, DateTime? fromUtc, CancellationToken ct = default)
    {
        var results =  await interviews.ListAsync(new InterviewByInterviewerSpecification(interviewerId, fromUtc), ct);
        var dtos = new List<InterviewDto>();

        foreach (var i in results)
        {
            dtos.Add(await ToDtoAsync(i, applications, ct));
        }

        return dtos;
    }

    public async Task<ServiceResult<InterviewDto>> RescheduleAsync(Guid id, RescheduleInterviewDto dto, CancellationToken ct = default)
    {
        var interview = await interviews.GetByIdAsync(id, ct);
        if (interview is null) return ServiceResult<InterviewDto>.Fail("Interview not found", ServiceErrorType.NotFound);

        if (interview.Status != InterviewStatus.Scheduled)
            return ServiceResult<InterviewDto>.Fail("Only  a Scheduled interview can be rescheduled.");

        interview.ScheduledAtUtc = dto.ScheduledAtUtc;
        interview.DurationMinutes = dto.DurationMinutes;
        interview.Location = dto.Location;

        await unit.Complete();
        var saved = await interviews.GetEntityWithSpec(new InterviewByIdWithRelationsSpecification(id), ct);
        return ServiceResult<InterviewDto>.Success(await ToDtoAsync(saved!, applications, ct));
    }

    public async Task<ServiceResult<InterviewDto>> ScheduleAsync(ScheduleInterviewDto dto, CancellationToken ct = default)
    {
        var application = await applications.GetByIdAsync(dto.ApplicationId, ct);
        if (application is null) return ServiceResult<InterviewDto>.Fail("Applcation not found.", ServiceErrorType.NotFound);

        if (await employees.GetByIdAsync(dto.InterviewerId, ct) is null)
            return ServiceResult<InterviewDto>.Fail("Selected interview was not found", ServiceErrorType.NotFound);

        if (dto.ScheduledAtUtc <= DateTime.UtcNow)
            return ServiceResult<InterviewDto>.Fail("Interview must be scheduled in the future.");

        var interview = new Interview
        {
            ApplicationId = dto.ApplicationId,
            Type = (InterviewType)dto.Type,
            ScheduledAtUtc = dto.ScheduledAtUtc,
            DurationMinutes = dto.DurationMinutes,
            Location = dto.Location,
            InterviewerId = dto.InterviewerId
        };

        timeline.Record(
            TimelineSubjectType.Applicant, 
            application.ApplicantId, 
            TimeLineEventType.Note, 
            $"{dto.Type} interview scheduled", 
            $"Scheduled for {dto.ScheduledAtUtc:yyyy-MM-dd HH:mm} UTC.");

        interviews.Add(interview);
        await unit.Complete();

        var saved = await interviews.GetEntityWithSpec(new InterviewByIdWithRelationsSpecification(interview.Id), ct);
        return ServiceResult<InterviewDto>.Success(await ToDtoAsync(saved!, applications, ct));
    }

    public async Task<ServiceResult<InterviewDto>> SubmitEvaluationAsync(Guid id, SubmitEvaluationDto dto, Guid submittedByUserId, CancellationToken ct = default)
    {
        var interview = await interviews.GetByIdAsync(id, ct);
        if (interview is null) return ServiceResult<InterviewDto>.Fail("Interview not found.", ServiceErrorType.NotFound);

        if (interview.Status is not (InterviewStatus.Completed or InterviewStatus.NoShow))
            return ServiceResult<InterviewDto>.Fail("An evaluation can only be submitted for a Completed or No-show interview");

        var existing = await evaluations.GetEntityWithSpec(
            new EvaluationByInterviewIdSpecification(id), ct);
        if (existing is not null)
            return ServiceResult<InterviewDto>.Fail("An evaluation has already been submitted for this interview.", ServiceErrorType.Conflict);

        var evaluation = new InterviewEvaluation
        {
            InterviewId = id,
            Rating = dto.Rating,
            Recommendation = (InterviewRecommendation)dto.Recommendation,
            Strenghts = dto.Strengths,
            Concerns = dto.Concerns,
            Notes = dto.Notes,
            SubmittedByUserId = submittedByUserId
        };

        evaluations.Add(evaluation);
        
        timeline.Record(
            TimelineSubjectType.Applicant,
            interview.ApplicationId,
            TimeLineEventType.Note,
            $"Interview evaluation submitted: {dto.Recommendation}",
            dto.Notes);

        await unit.Complete();
        var saved = await interviews.GetEntityWithSpec(new InterviewByIdWithRelationsSpecification(id), ct);
        return ServiceResult<InterviewDto>.Success(await ToDtoAsync(saved!, applications, ct));
    }

    private static async Task<InterviewDto> ToDtoAsync(Interview i, IGenericRepository<Core.Entities.Application> applications, CancellationToken ct)
    {
        var application = await applications.GetEntityWithSpec(new ApplicationByIdWithRelationsSpecification(i.ApplicationId), ct);

        InterviewEvaluationDto? evalDto = i.Evaluation is null ? null : new(
            i.Evaluation.Id,
            i.Evaluation.Rating,
            (int)i.Evaluation.Recommendation,
            i.Evaluation.Strenghts,
            i.Evaluation.Concerns,
            i.Evaluation.Notes,
            i.Evaluation.SubmittedByUserId,
            i.Evaluation.SubmittedAtUtc);

        return new InterviewDto(
            i.Id,
            i.ApplicationId,
            $"{application!.Applicant.FirstName} {application.Applicant.LastName}",
            application.JobPosting.Title,
            (int)i.Type,
            i.ScheduledAtUtc,
            i.DurationMinutes,
            i.Location,
            i.Id,
            $"{i.Interviewer.FirstName} {i.Interviewer.LastName}",
            (int)i.Status,
            i.CancellationReason,
            evalDto
        );
    }
}