using System.Text.Json;
using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Persistence;
public sealed class AuditSaveChangesInterceptor(
    ICurrentUserService currentUser, ITenantContext tenant) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        AddAuditEntries(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        AddAuditEntries(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void AddAuditEntries(DbContext? context)
    {
        if (context is null) return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditable &&
                e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (entries.Count == 0) return;

        foreach (var entry in entries)
        {
            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.TenantId,
                UserId = currentUser.UserId,
                EntityName = entry.Entity.GetType().Name,
                EntityId = GetPrimaryKeyValue(entry),
                IpAddress = currentUser.IpAddress,
                UserAgent = currentUser.UserAgent,
                TimestampUtc = DateTime.UtcNow
            };

            switch (entry.State)
            {
                case EntityState.Added:
                    log.Action = "Created";
                    log.NewValues = Serialize(entry.CurrentValues.Properties
                        .ToDictionary(p => p.Name, p => entry.CurrentValues[p]));
                    break;
                
                case EntityState.Deleted:
                    log.Action = "Deleted";
                    log.OldValues = Serialize(entry.OriginalValues.Properties
                        .ToDictionary(p => p.Name, p => entry.OriginalValues[p]));
                    break;

                case EntityState.Modified:
                    var changed = entry.Properties.Where(p => p.IsModified).ToList();
                    if (changed.Count == 0) continue;

                    var isSoftDelete = entry.Entity is ISoftDelete { IsDeleted: true} &&
                        changed.Any(p => p.Metadata.Name == nameof(ISoftDelete.IsDeleted));

                    log.Action = isSoftDelete ? "Deleted" : "Updated";
                    log.ChangedColumns = string.Join(",", changed.Select(p => p.Metadata.Name));
                    log.OldValues = Serialize(changed.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
                    log.NewValues = Serialize(changed.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
                    break;
            }

            context.Set<AuditLog>().Add(log);
        }
    }

    private static string GetPrimaryKeyValue(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry) =>
        entry.Metadata.FindPrimaryKey()?.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "")
            .FirstOrDefault() ?? "unknown";

    private static string Serialize(object values) => JsonSerializer.Serialize(values);
}