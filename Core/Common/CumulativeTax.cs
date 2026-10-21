namespace Core.Common;
public static class CumulativeTax
{
    public static decimal ThisPeriod(decimal cumulativeTax, decimal alreadyWithheld) =>
        Math.Max(0m, cumulativeTax - alreadyWithheld);
}