using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Employees;

public sealed record IdentifiersDto(
    Guid EmployeeId,
    string? Tin,
    string? Sss,
    string? PhilHealth,
    string? PagIbig,
    string? BankName,
    string? BankAccountName,
    string? BankAccountNumber,
    DateTime? UpdatedAtUtc);

public sealed class UpdateIdentifiersDto
{
    [MaxLength(50)]
    public string? Tin { get; init; }
    [MaxLength(50)]
    public string? Sss { get; init; }
    [MaxLength(50)]
    public string? PhilHealth { get; init; }
    [MaxLength(50)]
    public string? PagIbig { get; init; }
    [MaxLength(50)]
    public string? BankAccountNumber { get; init; }
    [MaxLength(100)]
    public string? BankName { get; init; }
    [MaxLength(150)]
    public string? BankAccountName { get; init; }
}

public sealed class RevealDto
{
    [Range(1, 5)]
    public int Field { get; init; }
}

public sealed record RevealedDto(int Field, string Value);
public sealed record IdentifierAccessDto(int Field, int Action, string UserName, DateTime CreatedAtUtc);