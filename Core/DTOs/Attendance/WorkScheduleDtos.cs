using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs.Attendance;
public sealed record WorkScheduleDto(
    Guid Id,
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int WorkingDays,
    bool IsActive,
    DateTime CreatedAtUtc);

public class CreateWorkScheduleDto
{
    [Required, MaxLength(100)]
    public string Name { get; init; } = "";
    [Required]
    public TimeOnly StartTime { get; init; }
    [Required]
    public TimeOnly EndTime { get; init; }
    [Required]
    public int WorkingDays { get; init; }
}

public class UpdateWorkScheduleDto : CreateWorkScheduleDto
{
    public bool IsActive { get; init; } = true;
}