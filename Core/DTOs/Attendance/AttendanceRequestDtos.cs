using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Attendance;

public sealed record AttendanceCorrectionDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeNumber,
    string EmployeeName,
    DateOnly Date,
    TimeOnly? RequestedClockIn,
    TimeOnly? RequestedClockOut,
    string Reason,
    int Status,
    bool EnteredByApprover,
    DateTime CreatedAtUtc,
    DateTime? ReviewdAtUtc,
    string? ReviewNote);

public sealed class CreateCorrectionDto
{
    [Required]
    public DateOnly? Date { get; init; }
    public TimeOnly? ClockIn { get; init; }
    public TimeOnly? ClockOut { get; init; }
    [Required, MaxLength(500)]
    public string Reason { get; init; } = "";
}

public sealed class ReviewDto
{
    [MaxLength(500)]
    public string? Note { get; init; }
}

public sealed record OvertimeRequestDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeNumber,
    string EmployeeName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int RequestedMinutes,
    int? ApprovedMinutes,
    string Reason,
    int Status,
    DateTime CreatedAtUtc,
    DateTime? ReviewedAtUtc,
    string? ReviewNote);

public sealed class CreateOvertimeDto
{
    [Required]
    public DateOnly? Date { get; init; }
    [Required]
    public TimeOnly? StartTime { get; init; }
    [Required]
    public TimeOnly? EndTime { get; init; }
    [Required, MaxLength(500)]
    public string Reason { get; init; } = "";
}

public sealed class ApproveOvertimeDto
{
    [Range(1, 720)]
    public int? ApprovedMinutes { get; init; }
    [MaxLength(500)]
    public string? Note { get; init; }
}