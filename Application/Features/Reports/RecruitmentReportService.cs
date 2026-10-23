using Core.Common;
using Core.DTOs.Reports;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Reports;

public sealed class RecruitmentReportService(
    IGenericRepository<Core.Entities.Application> applications,
    IGenericRepository<Interview> interviews,
    IGenericRepository<JobOffer> offers,
    IGenericRepository<JobPosting> postings,
    IFeatureGate featureGate) : IRecruitmentReportService
{
    public async Task<ServiceResult<CsvFile>> ExportAsync(RecruitmentReportQuery query, CancellationToken ct = default)
    {
        var built = await GetAsync(query, ct);
        if (!built.Succeeded) return ServiceResult<CsvFile>.Fail(built.Error!, built.ErrorType);
        var r = built.Value!;

        var rows = new List<string?[]>();
        rows.AddRange(r.Pipeline.Select(p => new string?[] { "Currently at stage", p.Stage, p.Count.ToString(), "" }));
        rows.AddRange(r.Conversion.Select(c => new string?[]
            { "Reached stage", c.Stage, c.Reached.ToString(), c.PercentOfApplied is decimal p ? $"{p}% of applied" : "" }));
        rows.AddRange(r.Offers.Select(o => new string?[] { "Offers", o.Label, o.Count.ToString(), "" }));
        rows.AddRange(r.Interviews.Select(i => new string?[] { "Interviews", i.Label, i.Count.ToString(), "" }));
        rows.AddRange(r.Postings.Select(p => new string?[] { "Applications by posting", p.Label, p.Count.ToString(), "" }));
        rows.Add(["Time to hire", "Hires measured", r.TimeToHire.Count.ToString(), ""]);
        rows.Add(["Time to hire", "Average days", r.TimeToHire.AverageDays?.ToString("0.#"), ""]);
        rows.Add(["Time to hire", "Median days", r.TimeToHire.MedianDays?.ToString("0.#"), ""]);

        var csv = WorkforceRules.ToCsv(["Section", "Item", "Count", "Note"], rows);
        return ServiceResult<CsvFile>.Success(new CsvFile($"recruitment-{r.From:yyyyMMdd}.csv", csv));
    }

    public async Task<ServiceResult<RecruitmentReportDto>> GetAsync(RecruitmentReportQuery query, CancellationToken ct = default)
    {
        await featureGate.EnsureFeatureEnabledAsync(PlanFeature.Ats, ct);

        var from = query.From!.Value;
        var to = query.To!.Value;
        var error = RecruitmentRules.CheckRange(from, to);
        if (error is not null) return ServiceResult<RecruitmentReportDto>.Fail(error);
        
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        
        var apps = (await applications.ListAsync(new ApplicationsInWindowSpecification(fromUtc, toUtc, query.JobPostingId, query.DepartmentId), ct)).ToList();
        var ids = apps.Select(a => a.Id).ToList();

        var interviewRows = ids.Count == 0 ? [] : await interviews.ListAsync(new InterviewsForApplicationsSpecification(ids), ct);
        var offerRows = ids.Count == 0 ? [] : await offers.ListAsync(new OffersForApplicationsSpecification(ids), ct);

        var interviewed = interviewRows.Select(i => i.ApplicationId).ToHashSet();
        var offered = offerRows.Select(o => o.ApplicationId).ToHashSet();

        var pipeline = RecruitmentRules.Pipeline_Counts(apps.Select(a => a.Status));
        var reach = RecruitmentRules.Reach(apps.Select(a =>
            RecruitmentRules.FurthestStage(a.Status, interviewed.Contains(a.Id), offered.Contains(a.Id))));

        var byApplication = apps.ToDictionary(a => a.Id);
        var hireDays = offerRows
            .Where(o => o.Status == JobOfferStatus.Accepted && o.RespondedAtUtc is not null && byApplication.ContainsKey(o.ApplicationId))
            .Select(o => RecruitmentRules.DaysBetween(
                DateOnly.FromDateTime(byApplication[o.ApplicationId].AppliedAtUtc),
                DateOnly.FromDateTime(o.RespondedAtUtc!.Value)));
        
        var offerCounts = offerRows.GroupBy(o => o.Status).OrderBy(g => g.Key)
            .Select(g => new CountByLabel(g.Key.ToString(), g.Count())).ToList();
        var accepted = offerRows.Count(o => o.Status == JobOfferStatus.Accepted);
        var declined = offerRows.Count(o => o.Status == JobOfferStatus.Declined);

        var interviewCounts = interviewRows.GroupBy(i => i.Status).OrderBy(g => g.Key)
            .Select(g => new CountByLabel(g.Key.ToString(), g.Count())).ToList();
        
        var postingCounts = apps.GroupBy(a => a.JobPosting.Title)
            .OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
            .Select(g => new CountByLabel(g.Key, g.Count())).ToList();

        return ServiceResult<RecruitmentReportDto>.Success(new RecruitmentReportDto(
            from,
            to,
            apps.Count,
            pipeline,
            reach,
            RecruitmentRules.Durations(hireDays),
            offerCounts,
            RecruitmentRules.Acceptance(accepted, declined),
            interviewCounts,
            postingCounts));
    }
}