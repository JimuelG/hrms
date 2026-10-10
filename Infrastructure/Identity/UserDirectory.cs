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
}