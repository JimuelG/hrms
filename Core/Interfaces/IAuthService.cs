using Core.DTOs.Auth;

namespace Core.Interfaces;
public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct);
    Task<AuthTokens?> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken ct);
    Task LogoutAsync(string refreshToken, CancellationToken ct);
}