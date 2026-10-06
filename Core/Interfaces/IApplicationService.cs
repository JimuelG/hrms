using Core.Common;
using Core.DTOs.Recruitment;
using Core.Enums;

namespace Core.Interfaces;
public interface IApplicationService
{
    Task<ServiceResult<ApplicationDto>> CreateAsync(CreateApplicationDto dto, CancellationToken ct = default);
    Task<ServiceResult<ApplicationDto>> UpdateStatusAsync(Guid id, UpdateApplicationStatusDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationDto>> GetByPostingAsync(Guid jobPostingId, ApplicationStatus? status, CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationDto>> GetByApplicantAsync(Guid applicantId, CancellationToken ct = default);
    Task<ApplicationDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}