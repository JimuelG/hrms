using Core.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Authorization;

public sealed class PermissionDirectory(AppDbContext db, ITenantContext tenant) : IPermissionDirectory
{
    public async Task<IReadOnlyDictionary<string, IReadOnlySet<Guid>>> GetHolderUserIdsAsync(IReadOnlyCollection<string> permissions, CancellationToken ct = default)
    {
        var wanted = permissions.Distinct().ToList();
        if (wanted.Count == 0) return new Dictionary<string, IReadOnlySet<Guid>>();

        var tenantId = tenant.RequiredTenantId;

        var activeMembers = db.UserTenants
            .Where(ut => ut.TenantId == tenantId && ut.IsActive)
            .Select(ut => ut.UserId);

        var rows = await db.UserTenantRoles
            .Where(utr => utr.TenantId == tenantId && activeMembers.Contains(utr.UserId))
            .SelectMany(utr => db.RolePermissions
                .Where(rp => rp.RoleId == utr.RoleId && wanted.Contains(rp.Permission.Code))
                .Select(rp => new { utr.UserId, rp.Permission.Code}))
            .Distinct()
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.Code)
            .ToDictionary(g => g.Key, g => (IReadOnlySet<Guid>)g.Select(r => r.UserId).ToHashSet());
    }
}