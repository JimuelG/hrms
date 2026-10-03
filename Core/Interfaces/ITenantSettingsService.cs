using Core.DTOs.Organization;
using Core.Entities;

namespace Core.Interfaces;
public interface ITenantSettingsService
{
    Task<TenantSettingsDto> GetAsync(CancellationToken ct = default);
    Task<TenantSettingsDto> UpdateAsync(UpdateTenantSettingsDto dto, CancellationToken ct = default);
}