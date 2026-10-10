using System.ComponentModel.DataAnnotations;
using Core.Common;

namespace Core.DTOs.Organization;
public sealed record BranchDto
(
    Guid Id,
    string Name,
    string Code,
    string? AddressLine,
    string? City,
    string? Country,
    string? TimeZoneId,
    bool IsActive,
    DateTime CreatedAtUtc
);

public sealed class CreateBranchDto
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = "";
    [Required, MaxLength(20)]
    public string Code { get; init; } = "";
    [MaxLength(300)]
    public string? AddressLine { get; init; }
    [MaxLength(100)]
    public string? City { get; init; }
    [MaxLength(100)]
    public string? Country { get; init; }
    [MaxLength(100)]
    [TimeZoneId]
    public string? TimeZoneId { get; init; }
}

public sealed class UpdateBranchDto
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = "";
    [Required, MaxLength(20)]
    public string Code { get; init; } = "";
    [MaxLength(300)]
    public string? AddressLine { get; init; }
    [MaxLength(100)]
    public string? City { get; init; }
    [MaxLength(100)]
    public string? Country { get; init; }
    [MaxLength(100)]
    [TimeZoneId]
    public string? TimeZoneId { get; init; }
    public bool IsActive { get; init; } = true;
}