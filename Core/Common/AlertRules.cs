namespace Core.Common;

public enum AlertKind { StaleApproval = 1, DocumentExpiry = 2, OpenAttendance = 3 }
public enum DocumentExpiryState { None = 0, Expiring = 1, Expired = 2 }

public sealed record DesiredAlert(AlertKind Kind, string Key, string Title, string Message, Guid? SubjectId);
public sealed record AlertPlan(IReadOnlyList<string> Create, IReadOnlyList<string> Resolve);

public static class AlertRules
{
    public const int StaleApprovalDays = 3;
    public const int DocumentWarnDays = 30;
    public const int OpenAttendanceDays = 2;

    public static bool IsStaleApproval(DateTime lastActivityUtc, DateTime nowUtc, int days) =>
        (nowUtc - lastActivityUtc).TotalDays >= days;

    public static DocumentExpiryState DocumentState(DateOnly? expires, DateOnly today, int warnDays)
    {
        if (expires is not DateOnly e) return DocumentExpiryState.None;
        if (e < today) return DocumentExpiryState.Expired;
        return e <= today.AddDays(warnDays) ? DocumentExpiryState.Expiring : DocumentExpiryState.None;
    }

    public static bool IsOpenAttendance(DateOnly recordDate, bool hasClockOut, DateOnly today, int days) =>
        !hasClockOut && today.DayNumber - recordDate.DayNumber >= days;

    public static string ApprovalKey(Guid id) => $"approval:{id:N}";
    public static string DocumentKey(Guid id, DocumentExpiryState s) => $"doc:{id:N}:{id:N}:{s}";
    public static string AttendanceKey(Guid id) => $"open-attendance:{id:N}";

    public static AlertPlan Reconcile(IEnumerable<string> openKeys, IEnumerable<string> desiredKeys)
    {
        var open = openKeys.ToHashSet();
        var desired = desiredKeys.ToHashSet();
        return new AlertPlan(
            desired.Except(open).OrderBy(k => k, StringComparer.Ordinal).ToList(),
            open.Except(desired).OrderBy(k => k, StringComparer.Ordinal).ToList());
    }
}