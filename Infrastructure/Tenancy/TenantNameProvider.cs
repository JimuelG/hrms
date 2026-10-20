using Core.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tenancy;

public sealed class TenantNameProvider(AppDbContext db, ITenantContext tenant) : ITenantNameProvider
{
    public Task<string?> GetCurrentNameAsync(CancellationToken ct = default) =>
        db.Tenants.Where(t => t.Id == tenant.RequiredTenantId).Select(t => (string?)t.Name).FirstOrDefaultAsync(ct);
}