using Core.Enums;

namespace Core.Common;
public static class PayslipRules
{
    public static bool EmployeeMayView(PayrollRunStatus status, string? errorMessage) =>
        status == PayrollRunStatus.Finalized && errorMessage is null;

    public static bool HrMayView(PayrollRunStatus status, string? errorMessage) =>
        status != PayrollRunStatus.Cancelled && errorMessage is null;
}