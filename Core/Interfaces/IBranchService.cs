using Core.Common;
using Core.DTOs.Organization;

namespace Core.Interfaces;
public interface IBranchService
{
    Task<ServiceResult<BranchDto>> CreateAsync(CreateBranchDto dto, CancellationToken ct = default);
    Task<ServiceResult<BranchDto>> UpdateAsync(Guid id, UpdateBranchDto dto, CancellationToken ct = default);
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<BranchDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<BranchDto>> GetAllAsync(string? search, CancellationToken ct = default);
}