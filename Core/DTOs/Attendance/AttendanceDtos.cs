using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Attendance;
public sealed class ClockRequestDto
{
    [Range(-90, 90)]
    public double? Latitude { get; init; }
    [Range(-180, 180)]
    public double? Longitude { get; init; }
    [Range(0.1, 100000)]
    public double? AccuracyMeters { get; init; }
    public Guid? WorkLocationId { get; init; }
}

public sealed record AttendanceRecordDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeNumber,
    string EmployeeName,
    DateOnly Date,
    DateTime ClockInUtc,
    string? ClockInLocationName,
    double? ClockInDistanceMeters,
    double? ClockInAccuracyMeters,
    double? ClockInLatitude,
    double? ClockInLongitude,
    DateTime? ClockOutUtc,
    string? ClockOutLocationName, 
    double? ClockOutDistanceMeters,
    double? ClockOutAccuracyMeters,
    double? ClockOutLatitude,
    double? ClockOutLongitude,
    int? WorkedMinutes,
    int Flags);