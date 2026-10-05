using Core.Common;
using Core.DTOs.Recruitment;
using Core.Enums;

namespace Core.Interfaces;
public interface IJobPostingService
{
    Task<ServiceResult<JobPostingDto>> CreateAsync(CreateJobPostingDto dto, CancellationToken ct = default);
    Task<ServiceResult<JobPostingDto>> UpdateAsync(Guid id, UpdateJobPostingDto dto, CancellationToken ct = default);
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<JobPostingDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<JobPostingDto>> GetAllAsync(string? search, JobPostingStatus? status, CancellationToken ct = default);
}