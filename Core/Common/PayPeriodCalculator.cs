namespace Core.Common;
public static class PayPeriodCalculator
{
    public static PayPeriod Containing(DateOnly date, PayFrequency frequency)
    {
        var first = new DateOnly(date.Year, date.Month, 1);
        var last = new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));

        if (frequency == PayFrequency.Monthly) return new(first, last);

        return date.Day <= 15
            ? new(first, new DateOnly(date.Year, date.Month, 15))
            : new(new DateOnly(date.Year, date.Month, 16), last);
    }

    public static PayPeriod Next(PayPeriod p, PayFrequency frequency) =>
        Containing(p.End.AddDays(1), frequency);

    public static IReadOnlyList<PayPeriod> Between(DateOnly from, DateOnly to, PayFrequency frequency)
    {
        var list = new List<PayPeriod>();
        if (to < from) return list;

        var p = Containing(from, frequency);
        while (p.Start <= to)
        {
            list.Add(p);
            p = Next(p, frequency);
        }

        return list;
    }

    public static decimal EmployedFraction(PayPeriod period, DateOnly hireDate, DateOnly? lastDay)
    {
        var start = hireDate > period.Start ? hireDate : period.Start;
        var end = lastDay is DateOnly l && l < period.End ? l : period.End;
        if (end < start) return 0m;

        var days = end.DayNumber - start.DayNumber + 1;
        return decimal.Round((decimal)days / period.Days, 10);
    }
}