using Core.Entities;

namespace Core.Interfaces;
public interface ITenantDirectory
{
    Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken ct = default);
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default);
    void Add(Tenant tenant);
    Task SaveChangesAsync(CancellationToken ct = default);
}