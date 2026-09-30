using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Auth;
public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = "";
    [Required, MaxLength(128)]
    public string Password { get; init; } = "";
    [MaxLength(63)]
    public string? TenantSlug { get; init; }
}

public sealed record TenantChoice(string Slug, string Name);
public enum LoginStatus { 
    Succeeded, 
    InvalidCredentials, 
    LockedOut, 
    TenantSelectionRequired 
}

public sealed record AuthTokens(
    string AccessToken, DateTime AccessTokenExpiresAtUtc,
    string RefreshToken, DateTime RefreshTokenExpiresAtUtc);

public sealed record LoginResult(
    LoginStatus Status, AuthTokens? Tokens = null, IReadOnlyList<TenantChoice>? Tenants = null);

public sealed record LoginResponse(
    string? AccessToken, DateTime? AccessTokenExpiresAtUtc,
    bool TenantSelectionRequired, IReadOnlyList<TenantChoice>? Tenants);