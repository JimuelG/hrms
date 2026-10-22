using Core.Enums;

namespace Core.Common;

public enum DashboardScopeKind { Company = 1, Team = 2, Personal = 3 }

public static class DashboardScope
{
    public static DashboardScopeKind Resolve(bool canSeeCompany, bool hasDirectReports) =>
        canSeeCompany ? DashboardScopeKind.Company
        : hasDirectReports ? DashboardScopeKind.Team
        : DashboardScopeKind.Personal;
}

public static class HeadcountRules
{
    public static bool IsWorking(EmployeeStatus status, DateOnly hire, DateOnly? lastDay, DateOnly today)
    {
        if (status is EmployeeStatus.Applicant or EmployeeStatus.PreOnboarding) return false;
        if (hire > today) return false;
        if (status is EmployeeStatus.Resigned or EmployeeStatus.Terminated)
            return lastDay is DateOnly l && l >= today;
        return lastDay is null || lastDay >= today;
    }
}

public enum LeaveWindow { None = 0, Today = 1, Upcoming = 2}

public static class LeaveWindows
{
    public static LeaveWindow Classify(DateOnly start, DateOnly end, DateOnly today, int upcomingDays)
    {
        if (end < today) return LeaveWindow.None;
        if (start <= today) return LeaveWindow.Today;
        return start <= today.AddDays(upcomingDays) ? LeaveWindow.Upcoming : LeaveWindow.None;
    }
}

public enum PresenceStatus { NotScheduled = 1, OnLeave = 2, Present = 3, Late = 4, NotYetIn = 5, NotIn = 6, NoSchedule = 7 }

public sealed record PresenceInput(
    bool WorksToday,
    bool OnLeave,
    bool HasSchedule,
    TimeOnly? ScheduledStart,
    TimeOnly? ClockInLocal,
    TimeOnly NowLocal,
    int GraceMinutes);

public sealed record PresenceTally(int Expected, int Present, int Late, int OnLeave, int NotYetIn, int NotIn, int NoSchedule);

public static class PresenceClassifier
{
    private static int Minutes(TimeOnly t) => t.Hour * 60 + t.Minute;

    public static PresenceStatus Classify(PresenceInput i)
    {
        if (i.ClockInLocal is TimeOnly cin)
            return i.ScheduledStart is TimeOnly s && Minutes(cin) - Minutes(s) > i.GraceMinutes
                ? PresenceStatus.Late : PresenceStatus.Present;

        if (i.OnLeave) return PresenceStatus.OnLeave;
        if (!i.WorksToday) return PresenceStatus.NotScheduled;
        if (!i.HasSchedule || i.ScheduledStart is null) return PresenceStatus.NoSchedule;

        return Minutes(i.NowLocal) <= Minutes(i.ScheduledStart.Value) + i.GraceMinutes
            ? PresenceStatus.NotYetIn : PresenceStatus.NotIn;
    }
    public static PresenceTally Tally(IEnumerable<PresenceStatus> statuses)
    {
        var list = statuses.ToList();
        int C(PresenceStatus s) => list.Count(x => x == s);
        var present = C(PresenceStatus.Present);
        var late = C(PresenceStatus.Late);
        var notYet = C(PresenceStatus.NotYetIn);
        var notIn = C(PresenceStatus.NotIn);
        return new(present + late + notYet + notIn, present, late, C(PresenceStatus.OnLeave), notYet, notIn, C(PresenceStatus.NoSchedule));
    }
}