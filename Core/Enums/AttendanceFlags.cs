namespace Core.Enums;
[Flags]
public enum AttendanceFlags
{
    None = 0,
    ClockInLocationUnverified = 1,
    ClockOutLocationUnverified = 2,
    LongShift = 4
}