using Core.Common;
using Core.DTOs.Recruitment;
using Core.Entities;

namespace Core.Interfaces;
public interface IApplicantService
{
    Task<ServiceResult<ApplicantDto>> CreateAsync(CreateApplicantDto dto, CancellationToken ct = default);
    Task<ServiceResult<ApplicantDto>> UpdateAsync(Guid id, UpdateApplicantDto dto, CancellationToken ct = default);
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<ApplicantDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ApplicantDto>> GetAllAsync(string? search, CancellationToken ct = default);

    Task<ServiceResult<ApplicantDto>> UploadResumeAsync(Guid applicantId, Stream content, string fileName, string contentType, long sizeBytes, CancellationToken ct = default);
    Task<ServiceResult<(Stream Content, string ContentType, string fileName)>> DownloadResumeAsync(
        Guid applicantId, CancellationToken ct = default);
}