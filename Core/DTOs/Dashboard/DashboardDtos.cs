namespace Core.DTOs.Dashboard;
public sealed record HeadcountCard(int Active, int? HiredThisMonth, int? LeftThisMonth);

public sealed record AttendanceCard(
    int Expected,
    int Present,
    int Late,
    int OnLeave,
    int NotYetIn,
    int NotIn,
    int NoSchedule,
    IReadOnlyList<string> LatePeople,
    IReadOnlyList<string> NotInPeople);

public sealed record AwayDto(string name, DateOnly From, DateOnly To);
public sealed record LeaveCard(IReadOnlyList<AwayDto> Today, IReadOnlyList<AwayDto> Upcoming);
public sealed record MeCard(int Status, int PendingLeaveRequests);

public sealed record DashboardDto(
    string Scope,
    DateOnly Date,
    HeadcountCard? Headcount,
    int PedingApprovals,
    AttendanceCard? Attendance,
    LeaveCard Leave,
    int? OpenJobPostings,
    MeCard? Me,
    int? OpenAlerts);