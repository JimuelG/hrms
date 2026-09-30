using System.Security.Cryptography;
using System.Text;
using Core.DTOs.Auth;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Identity;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    AppDbContext db,
    JwtTokenService jwt,
    IOptions<JwtOptions> options) : IAuthService
{
    private sealed record MemberShip(Guid TenantId, string Slug, string Name);
    public async Task<LoginResult> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null) return new(LoginStatus.InvalidCredentials);

        var check = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (check.IsLockedOut) return new(LoginStatus.LockedOut);
        if (!check.Succeeded) return new(LoginStatus.InvalidCredentials);

        var slug = request.TenantSlug?.Trim().ToLowerInvariant();

        if (user.IsPlatformAdmin && string.IsNullOrEmpty(slug))
        {
            var (platformTokens, _) = Issue(user, null, Guid.NewGuid(), ipAddress);
            await db.SaveChangesAsync(ct);
            return new(LoginStatus.Succeeded, platformTokens);
        }

        var memberships = await db.UserTenants
            .Where(ut => ut.UserId == user.Id && ut.IsActive && ut.Tenant.Status == TenantStatus.Active)
            .Select(ut => new MemberShip(ut.TenantId, ut.Tenant.Slug, ut.Tenant.Name))
            .ToListAsync(ct);

        if (memberships.Count == 0) return new(LoginStatus.InvalidCredentials);

        MemberShip? chosen;
        if (string.IsNullOrEmpty(slug))
        {
            if (memberships.Count > 1)
                return new(LoginStatus.TenantSelectionRequired,
                    Tenants: memberships.Select(m => new TenantChoice(m.Slug, m.Name)).ToList());
            chosen = memberships[0];
        }
        else
        {
            chosen = memberships.FirstOrDefault(m => m.Slug == slug);
            if (chosen is null) return new(LoginStatus.InvalidCredentials);   // don't reveal which tenants exist
        }

        var (tokens, _) = Issue(user, chosen.TenantId, Guid.NewGuid(), ipAddress);
        await db.SaveChangesAsync(ct);
        return new(LoginStatus.Succeeded, tokens);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct)
    {
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == Hash(refreshToken), ct);
        if (stored is not null) await RevokeFamilyAsync(stored.FamilyId, ct);
    }

    public async Task<AuthTokens?> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken ct)
    {
        var hash = Hash(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is null) return null;

        if (stored.IsRevoked)
        {
            // An already-used token came back: assume theft, kill the whole session family.
            await RevokeFamilyAsync(stored.FamilyId, ct);
            return null;
        }
        if (stored.IsExpired) return null;

        var user = await userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null || await userManager.IsLockedOutAsync(user)) return null;

        // Re-validate access: membership and tenant status may have changed since login.
        if (stored.TenantId is Guid tenantId)
        {
            var stillMember = await db.UserTenants.AnyAsync(ut =>
                ut.UserId == user.Id && ut.TenantId == tenantId &&
                ut.IsActive && ut.Tenant.Status == TenantStatus.Active, ct);
            if (!stillMember)
            {
                await RevokeFamilyAsync(stored.FamilyId, ct);
                return null;
            }
        }
        else if (!user.IsPlatformAdmin)
        {
            return null;
        }

        var (tokens, newHash) = Issue(user, stored.TenantId, stored.FamilyId, ipAddress);
        stored.RevokedAtUtc = DateTime.UtcNow;
        stored.ReplacedByTokenHash = newHash;
        await db.SaveChangesAsync(ct);   // revoke old + insert new atomically
        return tokens;
    }

    private (AuthTokens Tokens, string RefreshHash) Issue(
        ApplicationUser user, Guid? tenantId, Guid familyId, string? ip)
    {
        var (access, accessExpires) = jwt.CreateAccessToken(user, tenantId);

        var raw = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
        var entity = new RefreshToken
        {
            UserId = user.Id,
            TenantId = tenantId,
            FamilyId = familyId,
            TokenHash = Hash(raw),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(options.Value.RefreshTokenDays),
            CreatedByIp = ip
        };
        db.RefreshTokens.Add(entity);

        return (new AuthTokens(access, accessExpires, raw, entity.ExpiresAtUtc), entity.TokenHash);
    }

    private Task<int> RevokeFamilyAsync(Guid familyId, CancellationToken ct) =>
        db.RefreshTokens.Where(t => t.FamilyId == familyId && t.RevokedAtUtc == null)
          .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, DateTime.UtcNow), ct);

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

}