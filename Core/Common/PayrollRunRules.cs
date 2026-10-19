using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Enums;

namespace Core.Common;
public static class PayrollRunRules
{
    public const int MaxAdjustmentLabel = 100;
    public const decimal MaxAdjustmentAmount = 10_000_000m;

    public static string? CheckPeriod(PayPeriod period, PayFrequency frequency, DateOnly today)
    {
        if (period.Start.Year < LeaveCalculator.MinYear || period.End.Year > LeaveCalculator.MaxYear)
            return "The period is out of range.";

        var expected = PayPeriodCalculator.Containing(period.Start, frequency);
        if (expected != period)
            return $"That isn't a {(frequency == PayFrequency.Monthly ? "monthly" : "semi-monthly")} pay period. " +
                   $"The period containing {period.Start:yyyy-MM-dd} is {expected.Start:yyyy-MM-dd} to {expected.End:yyyy-MM-dd}.";

        if (period.Start > today.AddDays(31))
            return "A payroll run can't be started for a period that begins more than a month ahead.";

        return null;
    }

    public static bool CanMove(PayrollRunStatus from, PayrollRunStatus to) => (from, to) switch
    {
        (PayrollRunStatus.Draft, PayrollRunStatus.Approved) => true,
        (PayrollRunStatus.Approved, PayrollRunStatus.Finalized) => true,
        (PayrollRunStatus.Approved, PayrollRunStatus.Draft) => true,
        (PayrollRunStatus.Draft, PayrollRunStatus.Cancelled) => true,
        _ => false
    };

    public static bool IsEditable(PayrollRunStatus status) => status == PayrollRunStatus.Draft;

    public static string? CheckAdjustment(string? label, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(label)) return "Give the adjustment a label.";
        if (label.Trim().Length > MaxAdjustmentLabel) return $"The label can be at most {MaxAdjustmentLabel} characters.";
        if (amount == 0m) return "The amount can't be zero.";
        if (Math.Abs(amount) > MaxAdjustmentAmount) return $"The amount can be at most {MaxAdjustmentAmount:N0}.";
        if (decimal.Round(amount, 2) != amount) return "The amount can have at most two decimal places.";
        return null;
    }

    public static string? CheckApprove(PayrollRunStatus status, Guid creator, Guid actor, int errorCount)
    {
        if (!CanMove(status, PayrollRunStatus.Approved)) return "Only a draft run can be approved.";
        if (creator == actor) return "You started this run, so someone else must approve it.";
        if (errorCount > 0)
            return $"{errorCount} employee(s) couldn't be computed. Fix them and recompute, then approve.";
        return null;
    }

    public static string? CheckFinalize(PayrollRunStatus status, Guid? approver, Guid actor)
    {
        if (!CanMove(status, PayrollRunStatus.Finalized)) return "Only an approved run can be finalized.";
        if (approver == actor) return "You approved this run, so someone else must finalize it.";
        return null;
    }
}

public static class ResultSnapshot
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public sealed record InputsSnapshot(
        decimal UnpaidLeaveDays,
        int ApprovedOvertimeMinutes,
        int WorkingDaysPerWeek,
        DateOnly HireDate,
        DateOnly? LastDay,
        decimal DailyRate,
        decimal HourlyRate);

    public static string Write<T>(T value) => JsonSerializer.Serialize(value, Options); 

    public static T Read<T>(string json, T fallback)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, Options) ?? fallback;
        } catch (JsonException)
        {
            return fallback;
        }
    }
}

