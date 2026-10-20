using Core.Common;
using Core.Enums;

namespace Tests;

public class PayslipRulesTests
{
    [Theory]
    [InlineData(PayrollRunStatus.Draft, false)]
    [InlineData(PayrollRunStatus.Approved, false)]
    [InlineData(PayrollRunStatus.Finalized, true)]
    [InlineData(PayrollRunStatus.Cancelled, false)]
    public void Employees_See_Only_Finalized_Runs(PayrollRunStatus status, bool expected) =>
        Assert.Equal(expected, PayslipRules.EmployeeMayView(status, null));

    [Fact] public void A_Row_With_An_Error_Is_Never_A_Payslip()
    {
        Assert.False(PayslipRules.EmployeeMayView(PayrollRunStatus.Finalized, "No salary"));
        Assert.False(PayslipRules.HrMayView(PayrollRunStatus.Draft, "No salary"));
    }

    [Theory]
    [InlineData(PayrollRunStatus.Draft, true)]
    [InlineData(PayrollRunStatus.Approved, true)]
    [InlineData(PayrollRunStatus.Finalized, true)]
    [InlineData(PayrollRunStatus.Cancelled, false)]
    public void Hr_Sees_Everything_Except_Cancelled(PayrollRunStatus status, bool expected) =>
        Assert.Equal(expected, PayslipRules.HrMayView(status, null));
}