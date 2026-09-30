using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Identity;
public sealed class JwtTokenService(IOptions<JwtOptions> options)
{
    public const string TenantClaim = "tenant_id";
    public const string PlatformAdminClaim = "platform_admin";

    public (string Token, DateTime ExpiresAtUtc) CreateAccessToken(ApplicationUser user, Guid? tenantId)
    {
        var o = options.Value;
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(o.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (tenantId is Guid tId)
            claims.Add(new Claim(TenantClaim, tId.ToString()));
        else if (user.IsPlatformAdmin)
            claims.Add(new Claim(PlatformAdminClaim, "true"));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.SigningKey));
        var token = new JwtSecurityToken(o.Issuer, o.Audience, claims,
            notBefore: now, expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}