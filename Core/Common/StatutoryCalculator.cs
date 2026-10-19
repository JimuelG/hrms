using Core.Enums;

namespace Core.Common;
public static class StatutoryCalculator
{
    public static StatutoryAmount Compute(RuleSetData rs, decimal amount)
    {
        if (amount <= 0m) return new(0m, 0m);
        return rs.Method == StatutoryMethod.Rate ? ByRate(rs, amount) : ByBrackets(rs, amount);
    }

    private static StatutoryAmount ByRate(RuleSetData rs, decimal amount)
    {
        var basis = amount;
        if (rs.BaseFloor is decimal floor && basis < floor) basis = floor;
        if (rs.BaseCeiling is decimal ceiling && basis > ceiling) basis = ceiling;
        return new(Money(basis * rs.EmployeeRate), Money(basis * rs.EmployerRate));
    }

    private static StatutoryAmount ByBrackets(RuleSetData rs, decimal amount)
    {
        var row = rs.Brackets.OrderBy(b => b.From).LastOrDefault(b => b.From <= amount);
        if (row is null) return new(0m, 0m);

        var employee = row.EmployeeFixed + (amount - row.From) * row.EmployeeRateOnExcess;
        return new(Money(employee), Money(row.EmployerFixed));
    }
    
    private static decimal Money(decimal v) => decimal.Round(v, 2, MidpointRounding.AwayFromZero);
}