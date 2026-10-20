using Core.Enums;

namespace Core.Common;
public static class EmployeeSeparationRules
{
    public static string? Check(EmployeeStatus status, DateOnly hireDate, DateOnly? lastWorkingDay)
    {
        if (lastWorkingDay is DateOnly last && last < hireDate)
            return $"The last working day can' be before the hire date ({hireDate:yyyy-MM-dd}).";

        if (lastWorkingDay is null && status is EmployeeStatus.Resigned or EmployeeStatus.Terminated)
            return "Set the last working day. Payroll needs it to pay the final period correctly.";

        return null;
    }
}