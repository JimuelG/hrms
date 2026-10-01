using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Organization;
public sealed record DepartmentDto
(
    Guid id,
    string Name,
    string Code,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc
);

public sealed class CreateDepartmentDto
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = "";
    [Required, MaxLength(20)]
    public string Code { get; init; } = "";
    [MaxLength(500)]
    public string? Description { get; init; }
}

public sealed class UpdateDepartmentDto
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = "";
    [Required, MaxLength(20)]
    public string Code { get; init; } = "";
    [MaxLength(500)]
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}