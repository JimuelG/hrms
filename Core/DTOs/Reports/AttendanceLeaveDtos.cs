using System.ComponentModel.DataAnnotations;
using Core.Common;

namespace Core.DTOs.Reports;

public sealed class AttendanceReportQuery
{
    [Required]
    public DateOnly? From { get; set; }
    [Required]
    public DateOnly? To { get; set; }
    [Range(0, 120)]
    public int GraceMinutes { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? BranchId { get; set; }
}

public sealed record AttendanceRowDto(
    Guid EmployeeId,
    string EmployeeNumber,
    string Name,
    string? Department,
    string? Branch,
    int ScheduledDays,
    int PresentDays,
    int AbsentDays,
    int LateCount,
    int LateMinutes,
    int UndertimeMinutes,
    int OvertimeMinutes,
    int OpenRecords,
    bool NoSchedule);

public sealed record AttendanceTotalsDto(
    int ScheduledDays,
    int PresentDays,
    int AbsentDays,
    int LateCount,
    int LateMinutes,
    int UndertimeMinutes, 
    int OvertimeMinutes,
    int OpenRecords);

public sealed record AttendanceReportDto(
    DateOnly From,
    DateOnly To,
    int GraceMinutes,
    IReadOnlyList<AttendanceRowDto> Rows,
    AttendanceTotalsDto Totals,
    int NoScheduleCOunt);

public sealed record LeaveUsageRowDto(
    Guid EmployeeId,
    string EmployeeNumber,
    string Name,
    string? Department,
    string LeaveTypeCode,
    string LeaveTypeName,
    decimal Approved,
    decimal Pending,
    decimal? Balance);

public sealed record LeaveTypeTotalDto(
    string Code,
    string Name,
    decimal Approved,
    decimal Pending);

public sealed record LeaveUsageReportDto(
    int Year,
    IReadOnlyList<LeaveUsageRowDto> Rows,
    IReadOnlyList<LeaveTypeTotalDto> Totals);

public sealed record LeaveCalendarDto(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<CalendarDay> Days);