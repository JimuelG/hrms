using Core.Common;
using Core.DTOs.Platform;

namespace Core.Interfaces;
public interface IPlatformTenantService
{
    Task<IReadOnlyList<TenantAdminDto>> GetAllAsync(CancellationToken ct = default);
    Task<TenantAdminDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ServiceResult<TenantAdminDto>> CreateAsync(CreateTenantAdminDto dto, CancellationToken ct = default);
    Task<ServiceResult<TenantAdminDto>> SuspendAsync(Guid id, CancellationToken ct = default);
    Task<ServiceResult<TenantAdminDto>> ReactivateAsync(Guid id, CancellationToken ct = default);
}