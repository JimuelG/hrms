using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Attendance;

public sealed record WorkLocationDto(
    Guid Id,
    string Name,
    int Type,
    double? Latitude,
    double? Longitude,
    int? RadiusMeters,
    Guid? BranchId,
    string? BranchName,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    bool IsActive,
    DateTime CreatedAtUtc);

public class CreateWorkLocationDto
{
    [Required, MaxLength(150)]
    public string Name { get; init; } = "";
    public int Type { get; init; }

    [Range(-90, 90)]
    public double? Latitude { get; init; }
    [Range(-180, 180)]
    public double? Longitude { get; init; }
    [Range(25, 5000)]
    public int? RadiusMeters { get; init; }

    public Guid? BranchId { get; init; }
    public DateOnly? ValidFrom { get; init; }
    public DateOnly? ValidTo { get; init; }
}

public sealed class UpdateWorkLocationDto : CreateWorkLocationDto
{
    public bool IsActive { get; init; } = true;
}

public sealed class SetEmployeeWorkLocationsDto
{
    [Required]
    public List<Guid>? WorkLocationIds { get; init; }
}