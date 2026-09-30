using Core.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Authorization;

public sealed class PermissionService(AppDbContext db, IMemoryCache cache)
    : IPermissionService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);
    private static string Key(Guid userId, Guid tenantId) => $"perms:{tenantId}:{userId}";
    public async Task<IReadOnlySet<string>> GetPermissionsAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
    {
        if (cache.TryGetValue(Key(userId, tenantId), out HashSet<string>? cached) && cached is not null)
            return cached;

        var codes = await db.UserTenantRoles
            .Where(utr => utr.UserId == userId && utr.TenantId == tenantId)
            .SelectMany(utr => db.RolePermissions
                .Where(rp => rp.RoleId == utr.RoleId)
                .Select(rp => rp.Permission.Code))
            .Distinct()
            .ToListAsync(ct);

        var set = codes.ToHashSet(StringComparer.Ordinal);
        cache.Set(Key(userId, tenantId), set, CacheTtl);
        return set;
    }

    public void InvalidateCache(Guid userId, Guid tenantId)
    {
        cache.Remove(Key(userId, tenantId));
    }
}