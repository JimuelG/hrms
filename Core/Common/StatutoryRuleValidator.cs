using System.Reflection.Metadata;
using Core.Enums;

namespace Core.Common;
public static class StatutoryRuleValidator
{
    public static string? Validate(
        StatutoryKind kind,
        StatutoryMethod method,
        PayFrequency? basis,
        DateOnly effectiveFrom,
        decimal employeeRate,
        decimal employerRate,
        decimal? floor,
        decimal? ceiling,
        IReadOnlyList<BracketRow> brackets)
    {
        if (!Enum.IsDefined(kind)) return "Choose a valid kind.";
        if (!Enum.IsDefined(method)) return "Choose a valid method";
        if (effectiveFrom.Year < LeaveCalculator.MinYear || effectiveFrom.Year > LeaveCalculator.MaxYear)
            return "The effective date is not out of range.";

        if (kind == StatutoryKind.WithholdingTax)
        {
            if (method != StatutoryMethod.Brackets) return "Withholding tax tables are bracket tables.";
            if (basis is null || !Enum.IsDefined(basis.Value))
                return "Say whether this tax table is for monthly or semi-monthly pay.";
        }
        else if (basis is not null)
        {
            return "Only withholding tax tables have a pay basis.";
        }

        if (method == StatutoryMethod.Rate)
        {
            if (brackets.Count > 0) return "A rate-based rule set can't also have brackets.";
            if (!InUnit(employeeRate) || !InUnit(employerRate))
                return "Rates must be between 0 and 1 (for example 0.025 for 2.5%).";
            if (employeeRate == 0m && employerRate == 0m) return "At least one rate must be above zero.";
            if (floor < 0m || ceiling < 0m) return "The floor and ceiling can't be negative.";
            if (floor is decimal f && ceiling is decimal c && c < f) return "The ceiling can't be below the floor.";
            return null;
        }

        return ValidateBrackets(brackets);
    }

    private static string? ValidateBrackets(IReadOnlyList<BracketRow> rows)
    {
        if (rows.Count == 0) return "Add at least one bracket.";

        var sorted = rows.OrderBy(r => r.From).ToList();
        if (sorted[0].From != 0m) return "The first bracket must start at 0.";

        for (var i = 0; i < sorted.Count; i++)
        {
            var row = sorted[i];
            var n = i + 1;
            var isLast = i == sorted.Count - 1;

            if (row.From < 0m || row.EmployeeFixed < 0m || row.EmployeeFixed < 0m)
                return $"Bracket {n}: amounts can't be negative.";
            if (!InUnit(row.EmployeeRateOnExcess))
                return $"Bracket {n}: the rate must be between 0 and 1 (for example 0.15 for 15%).";

            if (row.To is  null)
            {
                if (!isLast) return $"Bracket {n}: only the last bracket can be open-ended.";
                continue;
            }

            if (row.To <= row.From) return $"Bracket {n}: 'to' must be greater than 'from'.";
            if (isLast) return "The last bracket must be open-ended (no 'to'), so every amount falls in a bracket.";

            var next = sorted[i + 1];
            if (next.From != row.To) 
                return $"Bracket {n} ends at {row.To:N2} but brackets {n + 1} starts at {next.From:N2}. " +
                    "Brackets must join exactly: use the next bracket's start as 'to' (for example 4,750.00, 4,749.99).";
        }

        return null;
    }

    private static bool InUnit(decimal r) => r >= 0m && r <= 1m;
}