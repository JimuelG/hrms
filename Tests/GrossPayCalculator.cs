using Core.Common;

namespace Tests;

public class GrossPayCalculatorTests
{
    private static readonly PayrollSettings Settings = new();
    private static readonly PayPeriod FirstHalf = new(new(2026, 10, 1), new(2026, 10, 15));
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    private static GrossInput Input(
        decimal monthly = 26000m, DateOnly? hired = null, DateOnly? last = null, PayPeriod? period = null,
        int days = 5, decimal unpaid = 0, int otMinutes = 0, SalaryPoint[]? history = null) =>
        new(history ?? [new SalaryPoint(D(2020, 1, 1), monthly)], hired ?? D(2020, 1, 1), last,
            period ?? FirstHalf, days, unpaid, otMinutes);

    private static decimal Line(GrossResult r, string code) => r.Lines.Single(l => l.Code == code).Amount;

    [Fact] public void A_Full_Semi_Monthly_Period_Pays_Half_The_Monthly_Salary()
    {
        var r = GrossPayCalculator.Calculate(Input(), Settings);
        Assert.Null(r.Error);
        Assert.Equal(13000m, r.Gross);
        Assert.Equal(13000m, Line(r, "BASIC"));
    }

    [Fact] public void Monthly_Frequency_Pays_The_Whole_Salary() =>
        Assert.Equal(26000m, GrossPayCalculator.Calculate(
            Input(period: new(D(2026, 10, 1), D(2026, 10, 31))),
            Settings with { Frequency = PayFrequency.Monthly }).Gross);

    [Fact] public void Rates_Follow_The_Working_Days_Factor()
    {
        var five = GrossPayCalculator.Calculate(Input(days: 5), Settings);
        Assert.Equal(1195.40m, five.DailyRate);
        Assert.Equal(149.43m, five.HourlyRate);

        var six = GrossPayCalculator.Calculate(Input(days: 6), Settings);
        Assert.Equal(996.81m, six.DailyRate);     // 26000 * 12 / 313 = 996.8051
    }

    [Fact] public void Unpaid_Leave_Takes_Off_The_Daily_Rate_Per_Day()
    {
        var r = GrossPayCalculator.Calculate(Input(unpaid: 2), Settings);
        Assert.Equal(-2390.80m, Line(r, "UNPAID_LEAVE"));
        Assert.Equal(13000m - 2390.80m, r.Gross);
    }

    [Fact] public void A_Half_Day_Of_Unpaid_Leave_Counts_As_Half()
    {
        var r = GrossPayCalculator.Calculate(Input(unpaid: 0.5m), Settings);
        Assert.Equal(-597.70m, Line(r, "UNPAID_LEAVE"));      // 1195.40 * 0.5
    }

    [Fact] public void Overtime_Is_Hours_Times_Hourly_Times_The_Multiplier()
    {
        var r = GrossPayCalculator.Calculate(Input(otMinutes: 120), Settings);
        Assert.Equal(373.58m, Line(r, "OVERTIME"));            // 149.43 * 1.25 * 2 = 373.575, away from zero
        Assert.Contains(r.Notes, n => n.Contains("ordinary-day rate"));
    }

    [Fact] public void Overtime_Minutes_Become_Fractions_Of_An_Hour()
    {
        var r = GrossPayCalculator.Calculate(Input(otMinutes: 90), Settings);
        Assert.Equal(280.18m, Line(r, "OVERTIME"));            // 149.43 * 1.25 * 1.5 = 280.18125
    }

    [Fact] public void Gross_Is_Exactly_The_Sum_Of_The_Printed_Lines()
    {
        var r = GrossPayCalculator.Calculate(Input(unpaid: 1.5m, otMinutes: 75), Settings);
        Assert.Equal(r.Lines.Sum(l => l.Amount), r.Gross);
    }

    [Fact] public void A_Mid_Period_Hire_Is_Prorated_And_Noted()
    {
        var r = GrossPayCalculator.Calculate(Input(hired: D(2026, 10, 8)), Settings);
        Assert.Equal(6933.33m, Line(r, "BASIC"));              // 13000 * 8/15
        Assert.Contains(r.Notes, n => n.Contains("Prorated"));
    }

    [Fact] public void A_Separation_Mid_Period_Is_Prorated()
    {
        var r = GrossPayCalculator.Calculate(Input(last: D(2026, 10, 5)), Settings);
        Assert.Equal(4333.33m, Line(r, "BASIC"));              // 13000 * 5/15
    }

    [Fact] public void Someone_Not_Employed_In_The_Period_Is_Refused() =>
        Assert.NotNull(GrossPayCalculator.Calculate(Input(hired: D(2026, 11, 1)), Settings).Error);

    [Fact] public void No_Salary_On_Record_Is_Refused() =>
        Assert.Contains("No salary", GrossPayCalculator.Calculate(Input(history: []), Settings).Error);

    [Fact] public void The_Salary_In_Force_At_The_Period_Start_Is_Used()
    {
        var history = new[] { new SalaryPoint(D(2020, 1, 1), 20000m), new SalaryPoint(D(2026, 9, 1), 30000m) };
        Assert.Equal(15000m, GrossPayCalculator.Calculate(Input(history: history), Settings).Gross);
    }

    [Fact] public void A_Mid_Period_Raise_Is_Not_Split_But_Is_Flagged()
    {
        var history = new[] { new SalaryPoint(D(2020, 1, 1), 20000m), new SalaryPoint(D(2026, 10, 10), 30000m) };
        var r = GrossPayCalculator.Calculate(Input(history: history), Settings);
        Assert.Equal(10000m, r.Gross);
        Assert.Contains(r.Notes, n => n.Contains("salary change"));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(7)]
    public void Unsupported_Work_Weeks_Are_Refused(int days) =>
        Assert.NotNull(GrossPayCalculator.Calculate(Input(days: days), Settings).Error);

    [Fact] public void Negative_Inputs_Are_Refused()
    {
        Assert.NotNull(GrossPayCalculator.Calculate(Input(unpaid: -1), Settings).Error);
        Assert.NotNull(GrossPayCalculator.Calculate(Input(otMinutes: -5), Settings).Error);
    }

    [Fact] public void A_Larger_Overtime_Multiplier_Is_Honoured()
    {
        var r = GrossPayCalculator.Calculate(Input(otMinutes: 60), Settings with { OrdinaryOvertimeMultiplier = 1.5m });
        Assert.Equal(224.15m, Line(r, "OVERTIME"));            // 149.43 * 1.5 = 224.145
    }
}