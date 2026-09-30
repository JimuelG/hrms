using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Persistence;
public sealed class TenantSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext _tenant;
    public TenantSaveChangesInterceptor(ITenantContext tenant) => _tenant = tenant;
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Enforce(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        Enforce(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Enforce(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries<ITenantEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.TenantId = _tenant.RequiredTenantId;
                    break;

                case EntityState.Modified:
                case EntityState.Deleted:
                    if (entry.Entity.TenantId != _tenant.RequiredTenantId)
                        throw new InvalidOperationException("Cross-tenant write blocked.");
                    if (entry.Property(nameof(ITenantEntity.TenantId)).IsModified)
                        throw new InvalidOperationException("TenantId cannot be changed.");
                    break;
            }
        }
    }
}