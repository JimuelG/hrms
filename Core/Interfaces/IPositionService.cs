using Core.Common;
using Core.DTOs.Organization;

namespace Core.Interfaces;
public interface IPositionService
{
Task<ServiceResult<PositionDto>> CreateAsync(CreatePositionDto dto, CancellationToken ct = default);
    Task<ServiceResult<PositionDto>> UpdateAsync(Guid id, UpdatePositionDto dto, CancellationToken ct = default);
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<PositionDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PositionDto>> GetAllAsync(string? search, CancellationToken ct = default);
    }