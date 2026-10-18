using Core.Common;

namespace Tests;

public class CompensationRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 15);
    private static readonly DateOnly Hired = new(2024, 3, 1);
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    private static string? Check(decimal monthly = 30000m, DateOnly? effective = null, DateOnly? latest = null) =>
        CompensationRules.Check(monthly, effective ?? D(2026, 11, 1), Hired, latest, Today);

    [Fact] public void An_Ordinary_Entry_Is_Accepted() => Assert.Null(Check());

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(10_000_000.01)]
    public void Out_Of_Range_Salaries_Are_Refused(double amount) => Assert.NotNull(Check((decimal)amount));

    [Fact] public void The_Maximum_Is_Allowed() => Assert.Null(Check(CompensationRules.MaxMonthly));

    [Fact] public void Three_Decimal_Places_Are_Refused() => Assert.Contains("two decimal", Check(30000.005m));

    [Fact] public void Two_Decimal_Places_Are_Fine() => Assert.Null(Check(30000.55m));

    [Fact] public void Before_The_Hire_Date_Is_Refused() =>
        Assert.Contains("hire date", Check(effective: D(2024, 2, 29)));

    [Fact] public void The_Hire_Date_Itself_Is_Allowed() => Assert.Null(Check(effective: Hired));

    [Fact] public void More_Than_A_Year_Ahead_Is_Refused()
    {
        Assert.Null(Check(effective: Today.AddDays(366)));
        Assert.Contains("a year ahead", Check(effective: Today.AddDays(367)));
    }

    [Fact] public void An_Entry_Must_Be_Strictly_After_The_Latest()
    {
        Assert.Contains("must be after", Check(effective: D(2026, 6, 1), latest: D(2026, 6, 1)));
        Assert.Contains("must be after", Check(effective: D(2026, 5, 1), latest: D(2026, 6, 1)));
        Assert.Null(Check(effective: D(2026, 6, 2), latest: D(2026, 6, 1)));
    }

    // ---- which salary applies on a date ----
    private static readonly SalaryPoint[] History =
    [
        new(D(2024, 3, 1), 20000m), new(D(2025, 7, 1), 25000m), new(D(2026, 11, 1), 30000m)
    ];

    [Fact] public void Before_The_First_Entry_There_Is_No_Salary() =>
        Assert.Null(CompensationRules.MonthlyOn(History, D(2024, 2, 28)));

    [Fact] public void The_Newest_Entry_That_Has_Started_Applies() =>
        Assert.Equal(25000m, CompensationRules.MonthlyOn(History, D(2026, 10, 31)));

    [Fact] public void A_Raise_Applies_On_Its_Effective_Date() =>
        Assert.Equal(30000m, CompensationRules.MonthlyOn(History, D(2026, 11, 1)));

    [Fact] public void Order_Of_The_Input_Does_Not_Matter() =>
        Assert.Equal(25000m, CompensationRules.MonthlyOn(History.Reverse(), D(2026, 1, 1)));

    [Fact] public void An_Empty_History_Has_No_Salary() =>
        Assert.Null(CompensationRules.MonthlyOn([], D(2026, 1, 1)));
}