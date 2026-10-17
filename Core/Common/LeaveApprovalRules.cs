using Core.Enums;

namespace Core.Common;
public static class LeaveApprovalRules
{
    public static string? CheckFinalApproval(ApprovalStatus requestStatus, EmployeeStatus employeeStatus, LeaveEntitlementMode mode, int leaveYear, int currentYear, decimal balance, decimal days, bool documentRequired, int documentCount)
    {
        if (requestStatus != ApprovalStatus.Pending)
            return "the leave request is no longer pending.";

        if (!LeaveEligibility.IsEmployed(employeeStatus))
            return "the employee's status no longer allows leave.";
        
        if (documentRequired && documentCount == 0)
            return "a supporting document is required and none is attached.";

        if (mode != LeaveEntitlementMode.Unlimited)
        {
            if (leaveYear < currentYear)
                return "the leave falls in a closed leave year. Reject it and ask HR to record it.";

            if (balance < days)
                return $"the balance is {Math.Max(balance, 0):0.#} day(s) but the request needs {days:0.#}.";
        }

        return null;
    }
}