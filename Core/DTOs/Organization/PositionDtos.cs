using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Organization;
public sealed record PositionDto(
    Guid Id,
    string Title,
    string Code,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed class CreatePositionDto
{
    [Required, MaxLength(200)]
    public string Title { get; init; } = "";
    [Required, MaxLength(20)]
    public string Code { get; init; } = "";
    [MaxLength(500)]
    public string? Description { get; init; }
}

public sealed class UpdatePositionDto
{
    [Required, MaxLength(200)]
    public string Title { get; init; } = "";
    [Required, MaxLength(20)]
    public string Code { get; init; } = "";
    [MaxLength(500)]
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}