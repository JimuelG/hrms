using System.Data.SqlTypes;
using Core.Enums;

namespace Core.Common;
public sealed record ThirteenthInput(decimal BasicEarned, decimal AlreadyPaid, decimal ExemptUsed, decimal? ExemptCeiling);

public sealed record ThirteenthAmount(
    decimal Entitlement,
    decimal Amount,
    decimal Exempt,
    decimal Taxable,
    IReadOnlyList<string> Warnings);

public sealed record ThirteenthInputsSnapshot(
    decimal BasicEarned,
    decimal AlreadyPaid,
    decimal Entitlement);

public static class ThirteenthMonthRules
{
    private static readonly HashSet<string> BasicCodes = ["BASIC", "UNPAID_LEAVE", "ABSENT", "LATE", "UNDERTIME"];

    public static decimal BasicEarned(IEnumerable<PayrollLine> lines) =>
        lines.Where(l => BasicCodes.Contains(l.Code)).Sum(l => l.Amount);

    public static string? CheckPeriod(int year, DateOnly cutoff, DateOnly today)
    {
        if (year < LeaveCalculator.MinYear || year > LeaveCalculator.MaxYear) return "The yaer is out of range.";
        if (cutoff.Year != year) return $"The cutoff date must fall in {year}.";
        if (cutoff.Day != DateTime.DaysInMonth(cutoff.Year, cutoff.Month))
            return "The cutoff must be the last day of a month, for example June 30 or December 31.";
        if (cutoff > today.AddDays(31)) return "A 13th month run can't be started for a cutoff more than a month ahead.";
        return null;
    }

    public static string? CheckExisting(IEnumerable<(DateOnly End, PayrollRunStatus Status)> existing, DateOnly cutoff)
    {
        var list = existing.OrderBy(e => e.End).ToList();

        foreach (var e in list.Where(e => e.End >= cutoff))
            return $"A 13th month run through {e.End:yyyy-MM-dd} already exists for this year. Cancel it first if you need to redo it.";
        
        foreach (var e in list.Where(e => e.Status != PayrollRunStatus.Finalized))
            return $"Finalize the earlier 13th month run (through {e.End:yyyy-MM-dd}) first, so the amount already paid is known.";

        return null;
    }
}

public static class ThirteenthMonthCalculator
{
    public static ThirteenthAmount Calculate(ThirteenthInput i)
    {
        var warnings = new List<string>();

        var entitlement = Money(Math.Max(0m, i.BasicEarned) / 12m);
        var due = entitlement - i.AlreadyPaid;
        if (due < 0m)
            warnings.Add($"{i.AlreadyPaid:N2} was already paid, which is more than the entitlement of {entitlement:N2}. Nothing more is due.");
        var amount = Math.Max(0m, due);

        decimal exempt;
        if (i.ExemptCeiling is decimal ceiling)
        {
            exempt = Math.Min(amount, Math.Max(0m, ceiling - i.ExemptUsed));
        }
        else
        {
            exempt = 0m;
            if (amount > 0m)
                warnings.Add("No tax-exempt ceiling is set in payroll settings, so the whole amount is treated as taxable.");
        }

        return new ThirteenthAmount(entitlement, amount, exempt, amount - exempt, warnings);
    }

    private static decimal Money(decimal v) => decimal.Round(v, 2, MidpointRounding.AwayFromZero);
}

public static class ThirteenthMonthComposer
{
    public static PayrollResult Compose(
        ThirteenthAmount a,
        IReadOnlyList<PayrollAdjustment> adjustments,
        TaxMethod method,
        RuleSetData? taxRule, YearToDate? ytd)
    {
        foreach (var adj in adjustments)
        {
            if (string.IsNullOrWhiteSpace(adj.Label)) return PayrollComposer.Failure("Every adjustment needs a label.");
            if (adj.Amount == 0m) return PayrollComposer.Failure($"The adjustmnet '{adj.Label.Trim()}' has no amount");
        }

        var lines = new List<PayrollLine>();
        var notes = new List<string>();
        var warnings = new List<string>(a.Warnings);

        if (a.Amount > 0m)
        {
            lines.Add(new("THIRTEENTH", "13th month pay", a.Amount, PayrollLineKind.Earning));
            notes.Add($"Entitlement {a.Entitlement:N2}. Tax-exempt part {a.Exempt:N2}, taxable part {a.Taxable:N2}.");
        }

        decimal tax = 0m;
        if (a.Taxable > 0m)
        {
            if (method != TaxMethod.Cumulative)
            {
                warnings.Add("The taxable part was recorded but no tax was withheld, because tax is set to per-period. " +
                                "Use cumulative tax, or add a deduction by hand.");
            }
            else if (taxRule is null)
            {
                warnings.Add("No withholding tax table is in force, so no tax was withheld on the taxable part.");
            }
            else if (taxRule.Basis is PayFrequency basis && basis != PayFrequency.Monthly)
            {
                return PayrollComposer.Failure("Cumulative tax needs a monthly withholding tax table.");
            }
            else
            {
                var y = ytd ?? new YearToDate(0m, 0m);
                var cumulative = StatutoryCalculator.Compute(taxRule, y.TaxableIncome + a.Taxable).Employee;
                tax = CumulativeTax.ThisPeriod(cumulative, y.TaxWithheld);
                notes.Add($"Tax is cumulative: year-to-date taxable income {y.TaxableIncome + a.Taxable:N2}, " +
                        $"tax withheld before this run {y.TaxWithheld:N2}.");
            }
        }

        if (tax > 0m) lines.Add(new("TAX", "Withholding tax", tax, PayrollLineKind.Deduction));

        foreach (var adj in adjustments)
        {
            var amount = decimal.Round(adj.Amount, 2, MidpointRounding.AwayFromZero);
            var label = adj.Label.Trim();
            lines.Add(amount > 0m
                ? new PayrollLine("ADJUSTMENT", label, amount, PayrollLineKind.Earning)
                : new PayrollLine("ADJUSTMENT", label, -amount, PayrollLineKind.Deduction));
        }

        if (adjustments.Count > 0 ) notes.Add("Adjustments don't change taxable income.");

        var earnings = lines.Where(l => l.Kind == PayrollLineKind.Earning).Sum(l => l.Amount);
        var deductions = lines.Where(l => l.Kind == PayrollLineKind.Deduction).Sum(l => l.Amount);
        var net = earnings - deductions;
        if (net < 0m) warnings.Add("Net pay is negative. Review the deductions and adjustments before finalizing.");

        return new PayrollResult(lines, a.Amount, a.Taxable, earnings, deductions, net, 0m, 0m, 0m, 0m, notes, warnings, null);
    }
}