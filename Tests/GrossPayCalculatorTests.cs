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
            period ?? FirstHalf, days, unpaid, OvertimeBuckets.Ordinary(otMinutes));

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
        Assert.Empty(r.Warnings);
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

    [Theory]
    [InlineData(4)]
    [InlineData(7)]
    public void Unsupported_Work_Weeks_Are_Refused(int days) =>
        Assert.NotNull(GrossPayCalculator.Calculate(Input(days: days), Settings).Error);

    [Fact] public void Negative_Inputs_Are_Refused()
    {
        Assert.NotNull(GrossPayCalculator.Calculate(Input(unpaid: -1), Settings).Error);
        Assert.NotNull(GrossPayCalculator.Calculate(Input() with { Overtime = [new(OvertimeDayType.Ordinary, -5)] }, Settings).Error);
    }

    [Fact] public void A_Larger_Overtime_Multiplier_Is_Honoured()
    {
        var r = GrossPayCalculator.Calculate(Input(otMinutes: 60), Settings with { OrdinaryOvertimeMultiplier = 1.5m });
        Assert.Equal(224.15m, Line(r, "OVERTIME"));            // 149.43 * 1.5 = 224.145
    }

    [Fact] public void A_Mid_Period_Raise_Is_Paid_By_Days_At_Each_Rate()
    {
        var history = new[] { new SalaryPoint(D(2020, 1, 1), 20000m), new SalaryPoint(D(2026, 10, 10), 30000m) };
        var r = GrossPayCalculator.Calculate(Input(history: history), Settings);

        Assert.Equal(12000m, r.Gross);                       // 9 days at 10,000/15 + 6 days at 15,000/15 = 6,000 + 6,000
        Assert.Equal(30000m, r.MonthlyBasic);                // the latest rate
        Assert.Contains(r.Notes, n => n.Contains("Salary changed"));
    }

    [Fact] public void A_Raise_On_The_First_Day_Is_Not_Reported_As_A_Change()
    {
        var history = new[] { new SalaryPoint(D(2020, 1, 1), 20000m), new SalaryPoint(D(2026, 10, 1), 30000m) };
        var r = GrossPayCalculator.Calculate(Input(history: history), Settings);

        Assert.Equal(15000m, r.Gross);
        Assert.DoesNotContain(r.Notes, n => n.Contains("Salary changed"));
    }

    [Fact] public void A_Raise_And_A_Separation_Together_Are_Both_Applied()
    {
        var history = new[] { new SalaryPoint(D(2020, 1, 1), 20000m), new SalaryPoint(D(2026, 10, 10), 30000m) };
        var r = GrossPayCalculator.Calculate(Input(history: history, last: D(2026, 10, 12)), Settings);
        Assert.Equal(9000m, r.Gross);                        // 6,000 + 3 days at 15,000/15
    }

    [Fact] public void A_Raise_After_The_Last_Day_Is_Ignored()
    {
        var history = new[] { new SalaryPoint(D(2020, 1, 1), 20000m), new SalaryPoint(D(2026, 10, 10), 30000m) };
        var r = GrossPayCalculator.Calculate(Input(history: history, last: D(2026, 10, 5)), Settings);

        Assert.Equal(3333.33m, r.Gross);                     // 5 days at 10,000/15
        Assert.Equal(20000m, r.MonthlyBasic);
    }

    [Fact] public void Unpaid_Leave_Uses_The_Latest_Daily_Rate_After_A_Raise()
    {
        var history = new[] { new SalaryPoint(D(2020, 1, 1), 20000m), new SalaryPoint(D(2026, 10, 10), 30000m) };
        var r = GrossPayCalculator.Calculate(Input(history: history, unpaid: 1), Settings);

        Assert.Equal(1379.31m, r.DailyRate);                 // 30,000 x 12 / 261
        Assert.Equal(-1379.31m, Line(r, "UNPAID_LEAVE"));
    }

    [Fact] public void Premium_Overtime_Uses_Its_Own_Multiplier_On_Its_Own_Line()
    {
        var settings = Settings with { RestDayOvertimeMultiplier = 1.3m, HolidayOvertimeMultiplier = 2m };
        var input = Input() with { Overtime = [new(OvertimeDayType.RestDay, 120), new(OvertimeDayType.Holiday, 60)] };

        var r = GrossPayCalculator.Calculate(input, settings);

        Assert.Equal(388.52m, r.Lines.Single(l => l.Label == "Overtime (rest day)").Amount);   // 149.43 x 1.3 x 2 = 388.518
        Assert.Equal(298.86m, r.Lines.Single(l => l.Label == "Overtime (holiday)").Amount);    // 149.43 x 2 x 1
        Assert.Empty(r.Warnings);
    }

    [Fact] public void An_Unset_Premium_Pays_At_The_Ordinary_Rate_And_Warns()
    {
        var input = Input() with { Overtime = [new(OvertimeDayType.Holiday, 60)] };
        var r = GrossPayCalculator.Calculate(input, Settings);

        Assert.Equal(186.79m, r.Lines.Single(l => l.Label == "Overtime (holiday)").Amount);    // 149.43 x 1.25 = 186.7875
        Assert.Contains(r.Warnings, w => w.Contains("holiday") && w.Contains("60 minutes"));
    }

    private static GrossInput WithAttendance(int absent = 0, int late = 0, int under = 0, bool on = true) =>
    Input() with { Attendance = new AttendanceDeductionInput(on, 0, absent, late, under, []) };

    [Fact] public void Off_By_Default_Changes_Nothing()
    {
        var r = GrossPayCalculator.Calculate(WithAttendance(absent: 3, late: 30, on: false), Settings);
        Assert.Equal(13000m, r.Gross);
        Assert.DoesNotContain(r.Lines, l => l.Code is "ABSENT" or "LATE" or "UNDERTIME");
    }

    [Fact] public void Absences_Late_And_Undertime_Each_Get_Their_Own_Line()
    {
        var r = GrossPayCalculator.Calculate(WithAttendance(absent: 2, late: 30, under: 60), Settings);

        Assert.Equal(-2390.80m, Line(r, "ABSENT"));        // 2 x 1,195.40
        Assert.Equal(-74.72m, Line(r, "LATE"));            // 149.43 x 30/60 = 74.715
        Assert.Equal(-149.43m, Line(r, "UNDERTIME"));      // 149.43 x 1
        Assert.Equal(r.Lines.Sum(l => l.Amount), r.Gross);
    }

    [Fact] public void Deductions_Are_Capped_At_The_Basic_Salary()
    {
        var r = GrossPayCalculator.Calculate(WithAttendance(absent: 20), Settings);

        Assert.Equal(0m, r.Gross);
        Assert.Equal(-13000m, Line(r, "ABSENT"));
        Assert.Contains(r.Warnings, w => w.Contains("capped"));
    }

    [Fact] public void Flags_From_Attendance_Are_Carried_As_Warnings()
    {
        var input = Input() with { Attendance = new AttendanceDeductionInput(true, 0, 0, 0, 0, ["Nov 3 has no clock-out."]) };
        Assert.Contains(GrossPayCalculator.Calculate(input, Settings).Warnings, w => w.Contains("no clock-out"));
    }
}