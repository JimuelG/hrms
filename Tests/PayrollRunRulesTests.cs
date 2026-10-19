using Core.Common;
using Core.Enums;

namespace Tests;

public class PayrollRunRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 18);
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    private static string? Check(DateOnly start, DateOnly end, PayFrequency f = PayFrequency.SemiMonthly) =>
        PayrollRunRules.CheckPeriod(new PayPeriod(start, end), f, Today);

    [Fact] public void A_Real_Semi_Monthly_Period_Is_Accepted()
    {
        Assert.Null(Check(D(2026, 10, 1), D(2026, 10, 15)));
        Assert.Null(Check(D(2026, 10, 16), D(2026, 10, 31)));
    }

    [Fact] public void A_Real_Monthly_Period_Is_Accepted() =>
        Assert.Null(Check(D(2026, 10, 1), D(2026, 10, 31), PayFrequency.Monthly));

    [Fact] public void A_Period_That_Is_Not_On_The_Calendar_Cuts_Is_Refused_With_The_Right_One()
    {
        var error = Check(D(2026, 10, 5), D(2026, 10, 20));
        Assert.Contains("isn't a semi-monthly", error);
        Assert.Contains("2026-10-01 to 2026-10-15", error);
    }

    [Fact] public void A_Month_Is_Not_A_Semi_Monthly_Period() =>
        Assert.NotNull(Check(D(2026, 10, 1), D(2026, 10, 31)));

    [Fact] public void A_Half_Month_Is_Not_A_Monthly_Period() =>
        Assert.NotNull(Check(D(2026, 10, 1), D(2026, 10, 15), PayFrequency.Monthly));

    [Fact] public void February_In_A_Leap_Year_Ends_On_The_29th() =>
    Assert.Null(PayrollRunRules.CheckPeriod(
        new PayPeriod(D(2028, 2, 16), D(2028, 2, 29)), PayFrequency.SemiMonthly, D(2028, 3, 5)));

    [Fact] public void A_Period_Starting_Within_A_Month_Is_Allowed_But_Further_Ahead_Is_Not()
    {
        Assert.Null(Check(D(2026, 11, 16), D(2026, 11, 30)));
        Assert.Contains("more than a month ahead", Check(D(2026, 12, 1), D(2026, 12, 15)));
    }

    [Fact] public void Past_Periods_Are_Allowed() => Assert.Null(Check(D(2025, 1, 1), D(2025, 1, 15)));

    [Fact] public void Years_Out_Of_Range_Are_Refused() =>
        Assert.Contains("out of range", Check(D(1999, 12, 16), D(1999, 12, 31)));

    [Theory]
    [InlineData(PayrollRunStatus.Draft, PayrollRunStatus.Approved, true)]
    [InlineData(PayrollRunStatus.Approved, PayrollRunStatus.Finalized, true)]
    [InlineData(PayrollRunStatus.Approved, PayrollRunStatus.Draft, true)]
    [InlineData(PayrollRunStatus.Draft, PayrollRunStatus.Cancelled, true)]
    [InlineData(PayrollRunStatus.Draft, PayrollRunStatus.Finalized, false)]      // approval can't be skipped
    [InlineData(PayrollRunStatus.Approved, PayrollRunStatus.Cancelled, false)]   // approved runs are sent back first
    [InlineData(PayrollRunStatus.Finalized, PayrollRunStatus.Draft, false)]
    [InlineData(PayrollRunStatus.Finalized, PayrollRunStatus.Approved, false)]
    [InlineData(PayrollRunStatus.Finalized, PayrollRunStatus.Cancelled, false)]
    [InlineData(PayrollRunStatus.Cancelled, PayrollRunStatus.Draft, false)]
    [InlineData(PayrollRunStatus.Draft, PayrollRunStatus.Draft, false)]
    public void Only_The_Defined_Transitions_Exist(PayrollRunStatus from, PayrollRunStatus to, bool allowed) =>
        Assert.Equal(allowed, PayrollRunRules.CanMove(from, to));

    [Fact] public void A_Finalized_Run_Has_No_Way_Out()
    {
        foreach (var to in Enum.GetValues<PayrollRunStatus>())
            Assert.False(PayrollRunRules.CanMove(PayrollRunStatus.Finalized, to));
    }

    [Theory]
    [InlineData(PayrollRunStatus.Draft, true)]
    [InlineData(PayrollRunStatus.Approved, false)]
    [InlineData(PayrollRunStatus.Finalized, false)]
    [InlineData(PayrollRunStatus.Cancelled, false)]
    public void Only_A_Draft_Is_Editable(PayrollRunStatus status, bool expected) =>
        Assert.Equal(expected, PayrollRunRules.IsEditable(status));

    [Fact] public void A_Reasonable_Adjustment_Passes()
    {
        Assert.Null(PayrollRunRules.CheckAdjustment("Allowance", 500m));
        Assert.Null(PayrollRunRules.CheckAdjustment("Cash advance", -200.50m));
    }

    [Fact] public void Bad_Adjustments_Are_Refused()
    {
        Assert.Contains("label", PayrollRunRules.CheckAdjustment("  ", 10m));
        Assert.Contains("zero", PayrollRunRules.CheckAdjustment("X", 0m));
        Assert.Contains("two decimal", PayrollRunRules.CheckAdjustment("X", 10.005m));
        Assert.Contains("at most", PayrollRunRules.CheckAdjustment("X", 10_000_001m));
        Assert.Contains("at most 100", PayrollRunRules.CheckAdjustment(new string('x', 101), 10m));
    }

    [Fact] public void The_Largest_Allowed_Adjustment_Passes() =>
        Assert.Null(PayrollRunRules.CheckAdjustment("X", -PayrollRunRules.MaxAdjustmentAmount));

    // ---- snapshots ----
    [Fact] public void A_Snapshot_Reads_Back_Exactly_What_Was_Written()
    {
        var lines = new List<PayrollLine>
        {
            new("BASIC", "Basic salary", 13000m, PayrollLineKind.Earning),
            new("TAX", "Withholding tax", 247.50m, PayrollLineKind.Deduction)
        };

        var back = ResultSnapshot.Read(ResultSnapshot.Write(lines), new List<PayrollLine>());

        Assert.Equal(lines, back);
    }

    [Fact] public void Inputs_Round_Trip_Including_Dates()
    {
        var inputs = new ResultSnapshot.InputsSnapshot(1.5m, 90, 5, new DateOnly(2020, 1, 15), null, 1195.40m, 149.43m);
        Assert.Equal(inputs, ResultSnapshot.Read(ResultSnapshot.Write(inputs), inputs with { UnpaidLeaveDays = 0m }));
    }

    [Fact] public void A_Damaged_Snapshot_Falls_Back_Instead_Of_Throwing()
    {
        var fallback = new List<string> { "fallback" };
        Assert.Equal(fallback, ResultSnapshot.Read("{ not json", fallback));
    }

    [Fact] public void Money_Survives_The_Round_Trip_Without_Drift() =>
        Assert.Equal(0.10m + 0.20m, ResultSnapshot.Read(ResultSnapshot.Write(0.10m + 0.20m), 0m));


    private static readonly Guid Maker = Guid.NewGuid(), Checker = Guid.NewGuid(), Payer = Guid.NewGuid();

    [Fact] public void The_Creator_Cannot_Approve_Their_Own_Run() =>
        Assert.Contains("someone else", PayrollRunRules.CheckApprove(PayrollRunStatus.Draft, Maker, Maker, 0));

    [Fact] public void A_Different_Person_Can_Approve() =>
        Assert.Null(PayrollRunRules.CheckApprove(PayrollRunStatus.Draft, Maker, Checker, 0));

    [Fact] public void A_Run_With_Uncomputable_Employees_Cannot_Be_Approved() =>
        Assert.Contains("2 employee(s)", PayrollRunRules.CheckApprove(PayrollRunStatus.Draft, Maker, Checker, 2));

    [Theory]
    [InlineData(PayrollRunStatus.Approved)]
    [InlineData(PayrollRunStatus.Finalized)]
    [InlineData(PayrollRunStatus.Cancelled)]
    public void Only_A_Draft_Can_Be_Approved(PayrollRunStatus status) =>
        Assert.Contains("Only a draft", PayrollRunRules.CheckApprove(status, Maker, Checker, 0));

    [Fact] public void The_Approver_Cannot_Finalize() =>
        Assert.Contains("someone else", PayrollRunRules.CheckFinalize(PayrollRunStatus.Approved, Checker, Checker));

    [Fact] public void The_Creator_May_Finalize_After_Someone_Else_Approved() =>
        Assert.Null(PayrollRunRules.CheckFinalize(PayrollRunStatus.Approved, Checker, Maker));

    [Theory]
    [InlineData(PayrollRunStatus.Draft)]
    [InlineData(PayrollRunStatus.Finalized)]
    [InlineData(PayrollRunStatus.Cancelled)]
    public void Only_An_Approved_Run_Can_Be_Finalized(PayrollRunStatus status) =>
        Assert.Contains("Only an approved", PayrollRunRules.CheckFinalize(status, Checker, Payer));
}