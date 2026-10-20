namespace Core.Common;
public static class OvertimeAttendanceCheck
{
    public static string? Check(DateOnly date, TimeOnly overtimeEnd, bool hasRecord, TimeOnly? clockOutLocal)
    {
        if (!hasRecord) return $"Approved overime on {date:MMM d} has no attendance record for that day.";
        if (clockOutLocal is null) return $"Approved overtime on {date:MMM d} has no clock-out recorded.";
        if (clockOutLocal.Value < overtimeEnd)
            return $"Approved overtime on {date:MMM d} runs to {overtimeEnd:HH:mm}, but the employee clocked out at {clockOutLocal.Value:HH:mm}.";
        return null;
    }
}