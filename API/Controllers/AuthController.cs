using Core.DTOs.Auth;
using Core.Interfaces;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;

public class AuthController(
    IAuthService auth,
    ITenantContext tenant,
    IOptions<JwtOptions> jwt) : BaseApiController
{
    private const string RefreshCookie = "hrms_refresh";
    private string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request, Ip, ct);

        switch (result.Status)
        {
            case LoginStatus.Succeeded:
                SetRefreshCookie(result.Tokens!);
                return Ok(ToResponse(result.Tokens!));
            case LoginStatus.TenantSelectionRequired:
                return Ok(new LoginResponse(null, null, true, result.Tenants));
            case LoginStatus.LockedOut:
                return Problem(statusCode: StatusCodes.Status423Locked,
                    title: "Account temparily locked. Try again later.");
            default:
                return Problem(statusCode: StatusCodes.Status401Unauthorized,
                    title: "Invalid email or password.");
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookie, out var token) || string.IsNullOrEmpty(token))
            return Unauthorized();

        var tokens = await auth.RefreshAsync(token, Ip, ct);
        if (tokens is null)
        {
            ClearRefreshCookie();
            return Unauthorized();
        }

        SetRefreshCookie(tokens);
        return Ok(ToResponse(tokens));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (Request.Cookies.TryGetValue(RefreshCookie, out var token) && !string.IsNullOrEmpty(token))
            await auth.LogoutAsync(token, ct);

        ClearRefreshCookie();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        UserId = User.FindFirst("sub")?.Value,
        TenantId = tenant.TenantId,
        IsPlatformAdmin = User.HasClaim(JwtTokenService.PlatformAdminClaim, "true")
    });

    private static LoginResponse ToResponse(AuthTokens t) =>
        new(t.AccessToken, t.AccessTokenExpiresAtUtc, false, null);

    private void SetRefreshCookie(AuthTokens t) =>
        Response.Cookies.Append(RefreshCookie, t.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth",
            Expires = t.RefreshTokenExpiresAtUtc
        });

    private void ClearRefreshCookie() =>
        Response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = "/api/v1/auth"});
}