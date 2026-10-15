using Core.Enums;

namespace Core.Common;

public sealed record BalanceSummary(
    decimal Granted,
    decimal CarriedOver,
    decimal Adjusted,
    decimal Used,
    decimal Balance);

public static class LeaveBalanceMath
{
    public static BalanceSummary Summarize(IEnumerable<LedgerRow> rows)
    {
        decimal granted = 0, carried = 0, adjusted = 0, usageNet = 0, balance = 0;

        foreach (var r in rows)
        {
            balance += r.Days;
            switch (r.Type)
            {
                case LeaveEntryType.Grant: 
                    granted += r.Days; 
                    break;
                case LeaveEntryType.CarryOver: 
                    carried += r.Days;
                    break;
                case LeaveEntryType.Adjustment: 
                    adjusted += r.Days;
                    break;
                case LeaveEntryType.Usage:
                case LeaveEntryType.Reversal:
                    usageNet += r.Days;
                    break;
            }
        }

        return new BalanceSummary(granted, carried, adjusted, 0m - usageNet, balance);
    }
}