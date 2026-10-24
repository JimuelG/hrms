using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Accounts;

public sealed record LoginAccountDto(
    bool HasLogin,
    string? Email,
    bool IsActive,
    bool MustChangePassword,
    bool AlsoInOtherCompanies);

public sealed record IssuedLoginDto(
    string Email,
    string? Temporary,
    DateTime? ExpiresAtUtc,
    bool ExistingAccount);

public sealed class CreateLoginDto
{
    [EmailAddress, MaxLength(256)]
    public string? Email { get; init; }
}