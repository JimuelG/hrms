using System.Net;
using Core.Common;
using Core.DTOs.Recruitment;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Recruitment;

public class JobOfferService(
    IUnitOfWork unit,
    IGenericRepository<JobOffer> offers,
    IGenericRepository<Core.Entities.Application> applications,
    ITimelineService timeline) : IJobOfferService
{
    public async Task<ServiceResult<JobOfferDto>> AcceptAsync(Guid id, CancellationToken ct = default)
    {
        var offer = await offers.GetByIdAsync(id, ct);
        if (offer is null) return ServiceResult<JobOfferDto>.Fail("Job offer not found", ServiceErrorType.NotFound);

        if (offer.Status != JobOfferStatus.Sent)
            return ServiceResult<JobOfferDto>.Fail("Only a Sent offer can be accepted.");

        offer.Status = JobOfferStatus.Accepted;
        offer.RespondedAtUtc = DateTime.UtcNow;

        await unit.Complete();

        var application = await applications.GetByIdAsync(offer.ApplicationId, ct);
        if (application is not null)
        {
            application.Status = ApplicationStatus.Hired;
            timeline.Record(
                TimelineSubjectType.Applicant,
                application.ApplicantId,
                TimeLineEventType.Note,
                "Job offer accepted",
                "Candidate accepted. Proceed with onboarding to create their employee record.");

            await unit.Complete();
        }

        var saved = await offers.GetByIdAsync(id, ct);
        return ServiceResult<JobOfferDto>.Success(await ToDtoAsync(saved!, applications, ct));
    }

    public async Task<ServiceResult<JobOfferDto>> CreateAsync(CreateJobOfferDto dto, Guid createdByUserId, CancellationToken ct = default)
    {
        var application = await applications.GetByIdAsync(dto.ApplicationId, ct);
        if (application is null) return ServiceResult<JobOfferDto>.Fail("Application not found", ServiceErrorType.NotFound);

        if (await offers.CountAsync(new ActiveJobOfferByApplicationSpecification(dto.ApplicationId), ct) > 0)
            return ServiceResult<JobOfferDto>.Fail("This application already has an active offer. Withdraw it before creating a new one.", ServiceErrorType.Conflict);

        var offer = new JobOffer
        {
            ApplicationId = dto.ApplicationId,
            ProposedSalary = dto.ProposedSalary,
            Currency = dto.Currency,
            ProposedStartDate = dto.ProposedStartDate,
            ExpiresOn = dto.ExpiresOn,
            Terms = dto.Terms,
            CreatedByUserId = createdByUserId
        };

        offers.Add(offer);
        await unit.Complete();

        var saved = await offers.GetByIdAsync(offer.Id, ct);
        return ServiceResult<JobOfferDto>.Success(await ToDtoAsync(saved!, applications, ct));
    }

    public async Task<ServiceResult<JobOfferDto>> DeclineAsync(Guid id, DeclineJobOfferDto dto, CancellationToken ct = default)
    {
        var offer = await offers.GetByIdAsync(id, ct);
        if (offer is null) return ServiceResult<JobOfferDto>.Fail("Job offer not found", ServiceErrorType.NotFound);

        if (offer.Status != JobOfferStatus.Sent)
            return ServiceResult<JobOfferDto>.Fail("Only a Sent offer can be declined.");

        offer.Status = JobOfferStatus.Declined;
        offer.RespondedAtUtc = DateTime.UtcNow;
        offer.DeclineReason = dto.DeclineReason;
        await unit.Complete();

        var application = await applications.GetByIdAsync(offer.ApplicationId, ct);
        if (application is not null)
        {
            timeline.Record(TimelineSubjectType.Applicant,
            application.ApplicantId,
            TimeLineEventType.Note,
            "Job offer declined",
            dto.DeclineReason);
            await unit.Complete();
        }

        var saved = await offers.GetByIdAsync(id, ct);
        return ServiceResult<JobOfferDto>.Success(await ToDtoAsync(saved!, applications, ct));
    }

    public async Task<IReadOnlyList<JobOfferDto>> GetByApplicationAsync(Guid applicationId, CancellationToken ct = default)
    {
        var results = await offers.ListAsync(new JobOfferByApplicationSpecification(applicationId), ct);
        var dtos = new List<JobOfferDto>();

        foreach (var o in results)
        {
            dtos.Add(await ToDtoAsync(o, applications, ct));
        }

        return dtos;
    }

    public async Task<ServiceResult<JobOfferDto>> SendAsync(Guid id, CancellationToken ct = default)
    {
        var offer = await offers.GetByIdAsync(id, ct);
        if (offer is null) return ServiceResult<JobOfferDto>.Fail("Job offer not found", ServiceErrorType.NotFound);

        if (offer.Status != JobOfferStatus.Draft)
            return ServiceResult<JobOfferDto>.Fail("Only a Draft offer can ba sent.");

        offer.Status = JobOfferStatus.Sent;
        offer.SendAtUtc = DateTime.UtcNow;
        await unit.Complete();

        var application = await applications.GetByIdAsync(offer.ApplicationId, ct);
        if (application is not null)
        {
            application.Status = ApplicationStatus.JobOffer;
            timeline.Record(
                TimelineSubjectType.Applicant,
                application.ApplicantId,
                TimeLineEventType.Note,
                "Job offer sent",
                $"{offer.ProposedSalary} {offer.Currency}, starting on {offer.ProposedStartDate:yyyy-MM-dd}");
            await unit.Complete();
        }

        var saved = await offers.GetByIdAsync(id, ct);
        return ServiceResult<JobOfferDto>.Success(await ToDtoAsync(saved!, applications, ct));
    }

    public async Task<ServiceResult<JobOfferDto>> WithdrawnAsync(Guid id, CancellationToken ct = default)
    {
        var offer = await offers.GetByIdAsync(id, ct);
        if (offer is null) return ServiceResult<JobOfferDto>.Fail("Job offer not found", ServiceErrorType.NotFound);

        if (offer.Status is not (JobOfferStatus.Draft or JobOfferStatus.Sent))
            return ServiceResult<JobOfferDto>.Fail("Only a Draft or Sent offer can be withdrawn");

        offer.Status = JobOfferStatus.Withdrawn;
        await unit.Complete();

        var saved = await offers.GetByIdAsync(id, ct);
        return ServiceResult<JobOfferDto>.Success(await ToDtoAsync(saved!, applications, ct));
    }

    private static async Task<JobOfferDto> ToDtoAsync(JobOffer o, IGenericRepository<Core.Entities.Application> applications, CancellationToken ct)
    {
        var application = await applications.GetEntityWithSpec(new ApplicationByIdWithRelationsSpecification(o.ApplicationId), ct);

        return new JobOfferDto(
            o.Id,
            o.ApplicationId,
            $"{application!.Applicant.FirstName} {application!.Applicant.LastName}",
            application.JobPosting.Title,
            o.ProposedSalary,
            o.Currency,
            o.ProposedStartDate,
            o.ExpiresOn,
            o.Terms,
            (int)o.Status,
            o.SendAtUtc,
            o.RespondedAtUtc,
            o.DeclineReason,
            o.CreatedAtUtc);
    }
}