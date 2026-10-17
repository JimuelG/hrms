using Core.Interfaces;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

public class UserDirectory(AppDbContext db, UserManager<ApplicationUser> users) : IUserDirectory
{
    public async Task<Guid?> FindActiveMemberIdAsync(string email, Guid tenantId, CancellationToken ct = default)
    {
        var normalized = users.NormalizeEmail(email);
        return await db.UserTenants
            .Where(ut => ut.TenantId == tenantId && ut.IsActive && ut.User.NormalizedEmail == normalized)
            .Select(ut => (Guid?)ut.UserId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IReadOnlyCollection<Guid> userIds, Guid tenantId, CancellationToken ct = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<Guid, string>();

        var rows = await db.UserTenants
            .Where(ut => ut.TenantId == tenantId && ids.Contains(ut.UserId))
            .Select(ut => new { ut.UserId, ut.User.FirstName, ut.User.LastName })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.UserId, r => $"{r.FirstName} {r.LastName}".Trim());
    }
}