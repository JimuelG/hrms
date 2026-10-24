using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Auth;
public sealed class ChangeTemporaryPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = default!;
    [Required]
    public string CurrentPassword { get; init; } = default!;
    [Required, StringLength(128, MinimumLength = 10)]
    public string NewPassword { get; init; } = default!;
    public string? TenantSlug { get; init; }
}