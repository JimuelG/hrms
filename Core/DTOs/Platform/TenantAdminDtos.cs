using System.ComponentModel.DataAnnotations;
using Core.Entities;

namespace Core.DTOs.Platform;
public sealed record TenantAdminDto(
    Guid Id,
    string Name,
    string Slug,
    int Status,
    DateTime CreatedAtUtc);

public sealed class CreateTenantAdminDto
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = "";
    [Required, MaxLength(63)]
    [RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$", ErrorMessage = "Slug must be lowercase letters, numbers, and single hypens only.")]
    public string Slug { get; init; } = "";
}