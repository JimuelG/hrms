using System.Drawing;
using Core.Common;
using Core.DTOs.Recruitment;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Recruitment;

public sealed class ApplicantService(
    IUnitOfWork unit,
    IGenericRepository<Applicant> applicants,
    IFileStorageService storage,
    ITenantContext tenant) : IApplicantService
{
    private static readonly HashSet<string> AllowedResumeTypes =
        [
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        ];

    private const long MaxResumeBytes = 5 * 1024 * 1024;

    public async Task<ServiceResult<ApplicantDto>> CreateAsync(CreateApplicantDto dto, CancellationToken ct = default)
    {
        if (await applicants.CountAsync(new ApplicantByEmailSpecification(dto.Email), ct) > 0)
            return ServiceResult<ApplicantDto>.Fail($"An applicant with email '{dto.Email}' already exists.", ServiceErrorType.Conflict);

        var applicant = new Applicant
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Phone = dto.Phone,
            SkillsSumarry = dto.SkillsSumarry,
            EducationSummary = dto.EducationSummary,
            ExperienceSummary = dto.ExperienceSummary
        };

        applicants.Add(applicant);
        await unit.Complete();
        return ServiceResult<ApplicantDto>.Success(ToDto(applicant));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var applicant = await applicants.GetByIdAsync(id , ct);
        if (applicant is null) return ServiceResult<bool>.Fail("Applicant not found.", ServiceErrorType.NotFound);

        applicants.Remove(applicant);
        await unit.Complete();
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<(Stream Content, string ContentType, string fileName)>> DownloadResumeAsync(Guid applicantId, CancellationToken ct = default)
    {
        var applicant = await applicants.GetByIdAsync(applicantId, ct);

        if (applicant?.ResumeStorageKey is null)
            return ServiceResult<(Stream, string, string)>.Fail("No resume on file.", ServiceErrorType.NotFound);

        var stored = await storage.GetAsync(applicant.ResumeStorageKey, ct);
        if (stored is null) return ServiceResult<(Stream, string, string)>.Fail("Stored file is missing.", ServiceErrorType.NotFound);

        return ServiceResult<(Stream, string, string)>.Success(
            (stored.Value.Content, applicant.ResumeContentType!, applicant.ResumeOriginalFileName!));
    }

    public async Task<IReadOnlyList<ApplicantDto>> GetAllAsync(string? search, CancellationToken ct = default) =>
        (await applicants.ListAsync(new ApplicantSearchSpecification(search), ct)).Select(ToDto).ToList();

    public async Task<ApplicantDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var applicant = await applicants.GetByIdAsync(id, ct);
        return applicant is null ? null : ToDto(applicant);
    }

    public async Task<ServiceResult<ApplicantDto>> UpdateAsync(Guid id, UpdateApplicantDto dto, CancellationToken ct = default)
    {
        var applicant = await applicants.GetByIdAsync(id, ct);
        if (applicant is null) return ServiceResult<ApplicantDto>.Fail("Applicant not found.", ServiceErrorType.NotFound);

        if (await applicants.CountAsync(new ApplicantByEmailSpecification(dto.Email, excludeId: id), ct) > 0)
            return ServiceResult<ApplicantDto>.Fail($"An applicant with email '{dto.Email}' already exists.", ServiceErrorType.Conflict);

        applicant.FirstName = dto.FirstName;
        applicant.LastName = dto.LastName;
        applicant.Email = dto.Email;
        applicant.Phone = dto.Phone;
        applicant.SkillsSumarry = dto.SkillsSumarry;
        applicant.EducationSummary = dto.EducationSummary;
        applicant.ExperienceSummary = dto.ExperienceSummary;

        await unit.Complete();
        return ServiceResult<ApplicantDto>.Success(ToDto(applicant));
    }

    public async Task<ServiceResult<ApplicantDto>> UploadResumeAsync(Guid applicantId, Stream content, string fileName, string contentType, long sizeBytes, CancellationToken ct = default)
    {
        var applicant = await applicants.GetByIdAsync(applicantId, ct);
        if (applicant is null) return ServiceResult<ApplicantDto>.Fail("Applicant not found", ServiceErrorType.NotFound);

        if (!AllowedResumeTypes.Contains(contentType))
            return ServiceResult<ApplicantDto>.Fail("Unsupported file type. Allowed: PDF, DOC, DOCX.");
        if (sizeBytes <= 0 || sizeBytes > MaxResumeBytes)
            return ServiceResult<ApplicantDto>.Fail("Resume must be between 1 byte and 5MB.");

        var safeFileName = Path.GetFileName(fileName);
        var storageKey = $"tenant/{tenant.RequiredTenantId}/applicants/{applicantId}/resume/{Guid.NewGuid()}-{safeFileName}";
        var saved = await storage.SaveAsync(storageKey, content, safeFileName, contentType, ct);


        applicant.ResumeOriginalFileName = saved.OriginalFileName;
        applicant.ResumeContentType = saved.ContentType;
        applicant.ResumeSizeBytes = saved.SizeBytes;
        applicant.ResumeStorageKey = saved.StorageKey;

        await unit.Complete();
        return ServiceResult<ApplicantDto>.Success(ToDto(applicant));
    }

    private static ApplicantDto ToDto(Applicant a) => new(
        a.Id,
        a.FirstName,
        a.LastName,
        a.Email,
        a.Phone,
        a.SkillsSumarry,
        a.EducationSummary,
        a.ExperienceSummary,
        a.ResumeOriginalFileName,
        a.CreatedAtUtc);
}