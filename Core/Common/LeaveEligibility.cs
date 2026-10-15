using Core.Entities;
using Core.Enums;

namespace Core.Common;
public static class LeaveEligibility
{
    private static readonly EmployeeStatus[] Employed =
        [EmployeeStatus.Probationary, EmployeeStatus.Regular, EmployeeStatus.OnLeave, EmployeeStatus.Suspended];

    public static bool IsEmployed(EmployeeStatus status) => Employed.Contains(status);
}