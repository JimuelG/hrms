using Core.Common;
using Core.DTOs.Organization;

namespace Core.Interfaces;
public interface IDepartmentService
{
    Task<ServiceResult<DepartmentDto>> CreateAsync(CreateDepartmentDto dto, CancellationToken ct = default);
    Task<ServiceResult<DepartmentDto>> UpdateAsync(Guid id, UpdateDepartmentDto dto, CancellationToken ct = default);
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<DepartmentDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DepartmentDto>> GetAllAsync(string? search, CancellationToken ct = default);
}