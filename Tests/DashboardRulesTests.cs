using Core.Common;
using Core.Enums;

namespace Tests;

public class DashboardRulesTests
{
    private static DateOnly D(int m, int d) => new(2026, m, d);
    private static readonly DateOnly Today = D(10, 22);

    // ---- scope ----
    [Fact] public void Company_Permission_Wins() =>
        Assert.Equal(DashboardScopeKind.Company, DashboardScope.Resolve(true, true));
    [Fact] public void A_Manager_Without_Company_Access_Sees_The_Team() =>
        Assert.Equal(DashboardScopeKind.Team, DashboardScope.Resolve(false, true));
    [Fact] public void Everyone_Else_Sees_Only_Themselves() =>
        Assert.Equal(DashboardScopeKind.Personal, DashboardScope.Resolve(false, false));

    // ---- headcount ----
    [Theory]
    [InlineData(EmployeeStatus.Regular, true)]
    [InlineData(EmployeeStatus.Probationary, true)]
    [InlineData(EmployeeStatus.OnLeave, true)]
    [InlineData(EmployeeStatus.Suspended, true)]
    [InlineData(EmployeeStatus.Applicant, false)]
    [InlineData(EmployeeStatus.PreOnboarding, false)]
    public void Only_Employed_Statuses_Are_Working(EmployeeStatus s, bool expected) =>
        Assert.Equal(expected, HeadcountRules.IsWorking(s, D(1, 1), null, Today));

    [Fact] public void Not_Yet_Hired_Is_Not_Working() =>
        Assert.False(HeadcountRules.IsWorking(EmployeeStatus.Regular, D(10, 23), null, Today));
    [Fact] public void Hired_Today_Is_Working() =>
        Assert.True(HeadcountRules.IsWorking(EmployeeStatus.Regular, Today, null, Today));
    [Fact] public void A_Last_Day_Today_Still_Counts_But_Yesterday_Does_Not()
    {
        Assert.True(HeadcountRules.IsWorking(EmployeeStatus.Regular, D(1, 1), Today, Today));
        Assert.False(HeadcountRules.IsWorking(EmployeeStatus.Regular, D(1, 1), D(10, 21), Today));
    }
    [Fact] public void Resigned_Works_Until_The_Last_Day()
    {
        Assert.True(HeadcountRules.IsWorking(EmployeeStatus.Resigned, D(1, 1), D(10, 31), Today));
        Assert.False(HeadcountRules.IsWorking(EmployeeStatus.Resigned, D(1, 1), D(10, 1), Today));
        Assert.False(HeadcountRules.IsWorking(EmployeeStatus.Terminated, D(1, 1), null, Today));
    }

    // ---- leave windows ----
    [Fact] public void Leave_That_Ended_Yesterday_Is_Not_Shown() =>
        Assert.Equal(LeaveWindow.None, LeaveWindows.Classify(D(10, 20), D(10, 21), Today, 7));
    [Fact] public void Leave_Covering_Today_Is_Today()
    {
        Assert.Equal(LeaveWindow.Today, LeaveWindows.Classify(Today, Today, Today, 7));
        Assert.Equal(LeaveWindow.Today, LeaveWindows.Classify(D(10, 20), D(10, 25), Today, 7));
        Assert.Equal(LeaveWindow.Today, LeaveWindows.Classify(D(10, 10), Today, Today, 7));
    }
    [Fact] public void Leave_Starting_Within_The_Window_Is_Upcoming()
    {
        Assert.Equal(LeaveWindow.Upcoming, LeaveWindows.Classify(D(10, 23), D(10, 24), Today, 7));
        Assert.Equal(LeaveWindow.Upcoming, LeaveWindows.Classify(D(10, 29), D(10, 30), Today, 7));
    }
    [Fact] public void Leave_Starting_After_The_Window_Is_Not_Shown() =>
        Assert.Equal(LeaveWindow.None, LeaveWindows.Classify(D(10, 30), D(10, 31), Today, 7));

    // ---- presence ----
    private static readonly TimeOnly Nine = new(9, 0);

    private static PresenceStatus Status(
        TimeOnly? clockIn = null, TimeOnly? now = null, bool works = true, bool leave = false,
        bool schedule = true, int grace = 10) =>
        PresenceClassifier.Classify(new(works, leave, schedule, schedule ? Nine : null, clockIn, now ?? new TimeOnly(12, 0), grace));

    [Fact] public void On_Time_Early_And_Within_Grace_Are_Present()
    {
        Assert.Equal(PresenceStatus.Present, Status(new TimeOnly(8, 30)));
        Assert.Equal(PresenceStatus.Present, Status(new TimeOnly(9, 0)));
        Assert.Equal(PresenceStatus.Present, Status(new TimeOnly(9, 10)));
    }
    [Fact] public void Past_The_Grace_Is_Late() =>
        Assert.Equal(PresenceStatus.Late, Status(new TimeOnly(9, 11)));
    [Fact] public void A_Clock_In_Beats_Leave_And_Rest_Days()
    {
        Assert.Equal(PresenceStatus.Present, Status(new TimeOnly(9, 0), leave: true));
        Assert.Equal(PresenceStatus.Present, Status(new TimeOnly(9, 0), works: false));
    }
    [Fact] public void A_Clock_In_Without_A_Schedule_Is_Present() =>
        Assert.Equal(PresenceStatus.Present, Status(new TimeOnly(9, 30), schedule: false));
    [Fact] public void Leave_Is_Shown_When_Nobody_Clocked_In() =>
        Assert.Equal(PresenceStatus.OnLeave, Status(leave: true));
    [Fact] public void A_Day_Off_Is_Not_Scheduled() =>
        Assert.Equal(PresenceStatus.NotScheduled, Status(works: false));
    [Fact] public void A_Working_Day_Without_A_Schedule_Is_Reported_As_Such() =>
        Assert.Equal(PresenceStatus.NoSchedule, Status(schedule: false));
    [Fact] public void Before_Start_Plus_Grace_Is_Not_Yet_In_And_After_Is_Not_In()
    {
        Assert.Equal(PresenceStatus.NotYetIn, Status(now: new TimeOnly(8, 50)));
        Assert.Equal(PresenceStatus.NotYetIn, Status(now: new TimeOnly(9, 10)));
        Assert.Equal(PresenceStatus.NotIn, Status(now: new TimeOnly(9, 11)));
    }

    [Fact] public void The_Tally_Counts_Each_Status_And_Ignores_Days_Off()
    {
        var t = PresenceClassifier.Tally([
            PresenceStatus.Present, PresenceStatus.Present, PresenceStatus.Late, PresenceStatus.NotIn,
            PresenceStatus.NotYetIn, PresenceStatus.OnLeave, PresenceStatus.NotScheduled, PresenceStatus.NoSchedule]);
        Assert.Equal(new PresenceTally(5, 2, 1, 1, 1, 1, 1), t);
    }
}