using Core.Enums;

namespace Core.Common;
public static class LeaveFilingRules
{
    public const int DaysBack = 30;
    public const int DaysAhead = 365;

    public static string? CheckDates(DateOnly start, DateOnly end, DateOnly today)
    {
        if (start.Year != end.Year)
            return "A request can't span two leave years. File one request for each year.";
        if (start.Year < today.Year)
            return "That date falls in a closed leave year. Ask HR to record it.";
        if (start < today.AddDays(-DaysBack))
            return $"Leave can only be filled up to {DaysBack} days back";
        if (end > today.AddDays(DaysAhead))
            return "Leave can only be filled up to a year ahead.";
        return null;
    }

    public static string? CheckEligibility(EmployeeStatus status, DateOnly hireDate, int minServiceMonths, string leaveTypeName, DateOnly start)
    {
        if (!LeaveEligibility.IsEmployed(status))
            return "Your employement status doesn't allow filing leave. Contact HR.";

        var eligibleForm = LeaveGrantPlanner.EligibleFrom(hireDate, minServiceMonths);

        return eligibleForm > start
            ? $"{leaveTypeName} needs {minServiceMonths} months of service. You're eligible from {eligibleForm:yyyy-MM-dd}."
            : null;
    }

    public static string? CheckBalance(LeaveEntitlementMode mode, decimal balance, decimal pendingDays, decimal requested)
    {
        if (mode == LeaveEntitlementMode.Unlimited) return null;

        var available = balance - pendingDays;
        if (requested <= available) return null;

        var shown = Math.Max(available, 0);
        return pendingDays > 0
            ? $"You have {shown:0.#} day(s) available ({pendingDays:0.#} already requested and waiting), and this request needs {requested:0.#}"
            : $"You have {shown:0.#} day(s) available, and this request needs {requested:0.#}.";
    }

    public static string? CheckFutureYear(LeaveEntitlementMode mode, int leaveYear, int currentYear)
    {
        if (mode != LeaveEntitlementMode.Annual || leaveYear <= currentYear) return null;
        return $"Leave for {leaveYear} opens on January 1, {leaveYear}. You can file it the, or ask HR if it's urgent.";
    }
}