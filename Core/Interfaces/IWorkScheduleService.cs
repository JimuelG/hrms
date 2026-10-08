using Core.Common;
using Core.DTOs.Attendance;

namespace Core.Interfaces;
public interface IWorkScheduleService
{
    Task<ServiceResult<WorkScheduleDto>> CreateAsync(CreateWorkScheduleDto dto, CancellationToken ct = default);
    Task<ServiceResult<WorkScheduleDto>> UpdateAsync(Guid id, UpdateWorkScheduleDto dto, CancellationToken ct = default);
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<WorkScheduleDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<WorkScheduleDto>> GetAllAsync(string? search, CancellationToken ct = default);
}