namespace Core.Common;
public static class AdjustmentRules
{
    public static decimal TaxableEffect(IEnumerable<PayrollAdjustment> adjustments) =>
        adjustments.Where(a => a.Tax == AdjustmentTax.Taxable).Sum(a => decimal.Round(a.Amount, 2, MidpointRounding.AwayFromZero));
}

public sealed record LoanState(decimal TotalOwed, decimal PaidSoFar)
{
    public decimal Balance => Math.Max(0m, TotalOwed - PaidSoFar);
}

public static class RecurringRules
{
    public static bool Applies(DateOnly from, DateOnly? to, bool active, PayPeriod period) =>
        active && from <= period.End && (to is null || to >= period.Start);

    public static decimal LoanInstalment(decimal scheduled, LoanState state) =>
        Math.Min(Math.Abs(scheduled), state.Balance);

    public static string? CheckRule(string? label, decimal amount, DateOnly from, DateOnly? to, decimal? totalOwed)
    {
        if (string.IsNullOrWhiteSpace(label)) return "Give the item a label.";
        if (amount == 0m) return "The amount can't be zero.";
        if (Math.Abs(amount) > PayrollRunRules.MaxAdjustmentAmount) return "The amount is too large.";
        if (decimal.Round(amount, 2) != amount) return "The amount can have at most two decimal places.";
        if (to is DateOnly t && t < from) return "The end date can't be before the start date.";
        if (totalOwed is decimal o)
        {
            if (amount > 0m) return "A loan is a deduction, so the instalment must be negative.";
            if (o <= 0m) return "The total owed must be more than zero.";
            if (Math.Abs(amount) > o) return "The instalment can't exceed the total owed.";
        }
        return null;
    }

    public static string? CheckFinalizeApplication(string label, decimal owed, decimal paid, decimal amount) =>
        paid + amount > owed
            ? $"'{label}' would be collected beyond what is owed (owed {owed:N2}, already paid {paid:N2}, this run {amount:N2}). Recompute the run."
            : null;
}