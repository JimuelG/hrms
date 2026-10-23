using System.ComponentModel.DataAnnotations;
using Core.Common;

namespace Core.DTOs.Reports;

public sealed class RecruitmentReportQuery
{
    [Required]
    public DateOnly? From { get; set; }
    [Required]
    public DateOnly? To { get; set; }
    public Guid? JobPostingId { get; set; }
    public Guid? DepartmentId { get; set; }
}

public sealed record CountByLabel(string Label, int Count);

public sealed record RecruitmentReportDto(
    DateOnly From,
    DateOnly To,
    int TotalApplications,
    IReadOnlyList<StageCount> Pipeline,
    IReadOnlyList<StageReach> Conversion,
    DurationStats TimeToHire,
    IReadOnlyList<CountByLabel> Offers,
    AcceptanceRate OfferAcceptance,
    IReadOnlyList<CountByLabel> Interviews,
    IReadOnlyList<CountByLabel> Postings);