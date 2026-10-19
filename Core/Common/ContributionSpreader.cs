namespace Core.Common;
public static class ContributionSpreader
{
    public static decimal ForPeriod(decimal monthlyAmount, PayFrequency frequency, bool isFirstHalf, ContributionDeduction policy)
    {
        if (frequency == PayFrequency.Monthly) return monthlyAmount;
        if (policy == ContributionDeduction.SecondPeriodOnly) return isFirstHalf ? 0m : monthlyAmount;

        var first = decimal.Round(monthlyAmount / 2m, 2, MidpointRounding.AwayFromZero);
        return isFirstHalf ? first : monthlyAmount - first;
    }
}