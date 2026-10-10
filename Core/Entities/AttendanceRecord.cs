using Core.Enums;
using Core.Interfaces;

namespace Core.Entities;
public class AttendanceRecord : BaseEntity, ITenantEntity, IAuditable
{
    public Guid TenantId { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;
    
    public DateOnly Date { get; set; }

    public DateTime ClockInUtc { get; set; }
    public Guid? ClockInWorkLocationId { get; set; }
    public string? ClockInLocationName { get; set; }
    public double? ClockInDistanceMeters { get; set; }
    public double? ClockInAccuracyMeters { get; set; }
    public double? ClockInLatitude { get; set; }
    public double? ClockInLongitude { get; set; }

    public DateTime? ClockOutUtc { get; set; }
    public Guid? ClockOutWorkLocationId { get; set; }
    public string? ClockOutLocationName { get; set; }
    public double? ClockOutDistanceMeters { get; set; }
    public double? ClockOutAccuracyMeters { get; set; }
    public double? ClockOutLatitude { get; set; }
    public double? ClockOutLongitude { get; set; }

    public TimeOnly? ScheduledStart { get; set; }
    public TimeOnly? ScheduleEnd { get; set; }

    public AttendanceFlags Flags { get; set; }
}