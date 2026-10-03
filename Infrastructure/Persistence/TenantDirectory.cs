using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class TenantDirectory(AppDbContext db) : ITenantDirectory
{
    public void Add(Tenant tenant) => db.Tenants.Add(tenant);

    public Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken ct = default) =>
        db.Tenants.OrderBy(t => t.Name).ToListAsync(ct).ContinueWith(t => (IReadOnlyList<Tenant>)t.Result, ct);

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}