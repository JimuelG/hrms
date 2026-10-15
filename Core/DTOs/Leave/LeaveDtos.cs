using System.ComponentModel.DataAnnotations;
using Core.Common;

namespace Core.DTOs.Leave;

public sealed record LeaveTypeDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsPaid,
    int EntitlementMode,
    decimal DefaultDays,
    bool ProrateFirstYear,
    decimal MaxCarryOversDays,
    int MinServiceMonths,
    bool AllowsHalfDay,
    int? RequiresDocumentAfterDays,
    bool IsActive,
    DateTime CreatedAtUtc);

public class CreateLeaveTypeDto
{
    [Required, MaxLength(20), RegularExpression("^[A-Za-z0-9_-]+$",
    ErrorMessage = "Code can contain letters, numbers, hypens and underscores only.")]
    public string Code { get; init; } = "";

    [Required, MaxLength(100)]
    public string Name { get; init; } = "";
    [MaxLength(500)]
    public string? Description { get; init; }

    public bool IsPaid { get; init; } = true;
    public int EntitlementMode { get; init; } = 1;

    public decimal DefaultDays { get; init; }
    public bool ProrateFirstYear { get; init; } = true;
    public decimal MaxCarryOversDays { get; init; }
    [Range(0, 600)]
    public int MinServiceMonths { get; init; }

    public bool AllowsHalfDay { get; init; } = true;
    [Range(0, 366)]
    public int? RequiresDocumentAfterDays { get; init; }
}

public sealed class UpdateLeaveTypeDto : CreateLeaveTypeDto
{
    public bool IsActive { get; init; } = true;
}

public sealed record HolidayDto(Guid Id, DateOnly Date, string Name, int Type);

public sealed class UpsertHolidayDto
{
    [Required]
    public DateOnly? Date { get; init; }
    [Required, MaxLength(150)]
    public string Name { get; init; } = "";
    public int Type { get; init; } = 1;
}

public sealed class LeavePreviewQuery
{
    [Required]
    public Guid? LeaveTypeId { get; set; }
    [Required]
    public DateOnly? StartDate { get; set; }
    [Required]
    public DateOnly? EndDate { get; set; }
    public int StartPortion { get; set; } = 1;
    public int EndPortion { get; set; } = 1;
}

public sealed record LeaveDayLineDto(
    DateOnly Date,
    decimal Value,
    int Status,
    string? Holidayname
);

public sealed record LeavePreviewDto(
    decimal Days,
    bool DocumentRequired,
    bool IsPaid,
    IReadOnlyList<LeaveDayLineDto> Lines);