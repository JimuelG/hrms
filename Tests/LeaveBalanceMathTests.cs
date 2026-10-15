using Core.Common;
using Core.Enums;

namespace Tests;

public class LeaveBalanceMathTests
{
    [Fact]
    public void The_Balance_Is_The_Sum_Of_Every_Entry()
    {
        var s = LeaveBalanceMath.Summarize(
        [
            new LedgerRow(2026, LeaveEntryType.Grant, 15, true),
            new LedgerRow(2026, LeaveEntryType.CarryOver, 5, true),
            new LedgerRow(2026, LeaveEntryType.Adjustment, -2, false),
            new LedgerRow(2026, LeaveEntryType.Usage, -6, false),
            new LedgerRow(2026, LeaveEntryType.Reversal, 1, false)
        ]);

        Assert.Equal(15m, s.Granted);
        Assert.Equal(5m, s.CarriedOver);
        Assert.Equal(-2m, s.Adjusted);
        Assert.Equal(5m, s.Used);      // 6 taken, 1 given back
        Assert.Equal(13m, s.Balance);
    }

    [Fact]
    public void An_Empty_Ledger_Is_All_Zeros()
    {
        var s = LeaveBalanceMath.Summarize([]);
        Assert.Equal(0m, s.Balance);
        Assert.Equal(0m, s.Used);
    }

    [Theory]
    [InlineData(EmployeeStatus.Probationary, true)]
    [InlineData(EmployeeStatus.Regular, true)]
    [InlineData(EmployeeStatus.OnLeave, true)]
    [InlineData(EmployeeStatus.Suspended, true)]
    [InlineData(EmployeeStatus.Applicant, false)]
    [InlineData(EmployeeStatus.PreOnboarding, false)]
    [InlineData(EmployeeStatus.Resigned, false)]
    [InlineData(EmployeeStatus.Terminated, false)]
    [InlineData(EmployeeStatus.Archived, false)]
    public void Only_Current_Employees_Accrue_Leave(EmployeeStatus status, bool expected) =>
        Assert.Equal(expected, LeaveEligibility.IsEmployed(status));
}