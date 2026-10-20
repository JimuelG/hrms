using Core.Common;
using Core.Enums;

namespace Tests;

public class EmployeeSeparationRulesTests
{
    private static readonly DateOnly Hired = new(2024, 3, 1);

    [Theory]
    [InlineData(EmployeeStatus.Resigned)]
    [InlineData(EmployeeStatus.Terminated)]
    public void A_Separated_Employee_Needs_A_Last_Day(EmployeeStatus status) =>
        Assert.Contains("last working day", EmployeeSeparationRules.Check(status, Hired, null));

    [Fact] public void A_Current_Employee_Needs_None() =>
        Assert.Null(EmployeeSeparationRules.Check(EmployeeStatus.Regular, Hired, null));

    [Fact] public void A_Last_Day_Can_Be_Set_While_Still_Employed() =>
        Assert.Null(EmployeeSeparationRules.Check(EmployeeStatus.Regular, Hired, new DateOnly(2027, 1, 31)));

    [Fact] public void A_Last_Day_Before_The_Hire_Date_Is_Refused() =>
        Assert.Contains("before the hire date", EmployeeSeparationRules.Check(EmployeeStatus.Resigned, Hired, new DateOnly(2024, 2, 29)));

    [Fact] public void The_Hire_Date_Itself_Is_Allowed() =>
        Assert.Null(EmployeeSeparationRules.Check(EmployeeStatus.Resigned, Hired, Hired));
}