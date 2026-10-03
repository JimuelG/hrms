using Core.Common;
using Core.DTOs.Platform;
using Core.Entities;
using Core.Interfaces;

namespace Application.Features.Platform;

public sealed class PlatformTenantService(ITenantDirectory directory)
    : IPlatformTenantService
{
    public async Task<ServiceResult<TenantAdminDto>> CreateAsync(CreateTenantAdminDto dto, CancellationToken ct = default)
    {
        var slug = dto.Slug.Trim().ToLowerInvariant();

        if (await directory.GetBySlugAsync(slug, ct) is not null)
            return ServiceResult<TenantAdminDto>.Fail($"Slug '{slug}' is already in use.", ServiceErrorType.Conflict);

        var tenant = new Tenant
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            Status = TenantStatus.Active
        };
        directory.Add(tenant);
        await directory.SaveChangesAsync(ct);

        return ServiceResult<TenantAdminDto>.Success(ToDto(tenant));
    }

    public async Task<IReadOnlyList<TenantAdminDto>> GetAllAsync(CancellationToken ct = default) => (await directory.GetAllAsync(ct)).Select(ToDto).ToList();

    public async Task<TenantAdminDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await directory.GetByIdAsync(id, ct);
        return tenant is null ? null : ToDto(tenant);
    }

    public Task<ServiceResult<TenantAdminDto>> ReactivateAsync(Guid id, CancellationToken ct = default) => SetStatusAsync(id, TenantStatus.Active, ct);

    public Task<ServiceResult<TenantAdminDto>> SuspendAsync(Guid id, CancellationToken ct = default) => SetStatusAsync(id, TenantStatus.Suspended, ct);

    private async Task<ServiceResult<TenantAdminDto>> SetStatusAsync(Guid id, TenantStatus status, CancellationToken ct)
    {
        var tenant = await directory.GetByIdAsync(id, ct);
        if (tenant is null)
            return ServiceResult<TenantAdminDto>.Fail("Tenant not found.", ServiceErrorType.NotFound);

        tenant.Status = status;
        await directory.SaveChangesAsync(ct);
        return ServiceResult<TenantAdminDto>.Success(ToDto(tenant));
    }

    private static TenantAdminDto ToDto(Tenant t) => new(
        t.Id,
        t.Name,
        t.Slug,
        (int)t.Status,
        t.CreatedAtUtc
    );
}