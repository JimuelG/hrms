using Core.Common;
using Core.DTOs.Attendance;

namespace Core.Interfaces;
public interface IWorkLocationService
{
    Task<ServiceResult<WorkLocationDto>> CreateAsync(CreateWorkLocationDto dto, CancellationToken ct = default);
    Task<ServiceResult<WorkLocationDto>> UpdateAsync(Guid id, UpdateWorkLocationDto dto, CancellationToken ct = default);
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<WorkLocationDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<WorkLocationDto>> GetAllAsync(string? search, int? type, CancellationToken ct = default);
}

public interface IEmployeeWorkLocationService
{
    Task<IReadOnlyList<WorkLocationDto>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task<ServiceResult<IReadOnlyList<WorkLocationDto>>> SetForEmployeeAsync(
        Guid employeeId, IReadOnlyCollection<Guid> workLocationIds, CancellationToken ct = default);
}