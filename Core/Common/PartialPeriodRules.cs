namespace Core.Common;
public static class PartialPeriodRules
{
    public static decimal Factor(PartialMonthContribution policy, decimal employedFraction)
    {
        if (employedFraction >= 1m) return 1m;

        return policy switch
        {
            PartialMonthContribution.FullPeriod => 1m,
            PartialMonthContribution.ByDaysEmployed => employedFraction,
            PartialMonthContribution.SkipIfPartial => 0m,
            _ => 1m
        };
    }

    public static decimal Apply(decimal amount, decimal factor) =>
        factor == 1m ? amount : decimal.Round(amount * factor, 2, MidpointRounding.AwayFromZero);
}