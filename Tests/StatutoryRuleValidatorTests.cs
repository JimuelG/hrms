using Core.Common;
using Core.Enums;

namespace Tests;

public class StatutoryRuleValidatorTests
{
    private static BracketRow B(decimal from, decimal? to, decimal fixedEe = 0m, decimal rate = 0m) => new(from, to, fixedEe, 0m, rate);

    private static string? V(
        IReadOnlyList<BracketRow>? rows = null, StatutoryKind kind = StatutoryKind.Sss,
        StatutoryMethod method = StatutoryMethod.Brackets, PayFrequency? basis = null, int year = 2026,
        decimal ee = 0m, decimal er = 0m, decimal? floor = null, decimal? ceiling = null) =>
        StatutoryRuleValidator.Validate(kind, method, basis, new DateOnly(year, 1, 1), ee, er, floor, ceiling, rows ?? []);

    private static readonly BracketRow[] Good = [B(0, 5000), B(5000, 10000), B(10000, null)];

    [Fact] public void A_Contiguous_Table_Is_Accepted() => Assert.Null(V(Good));
    [Fact] public void A_Single_Open_Bracket_Is_Accepted() => Assert.Null(V([B(0, null)]));
    [Fact] public void Input_Order_Does_Not_Matter() => Assert.Null(V([B(10000, null), B(0, 5000), B(5000, 10000)]));

    [Fact] public void An_Empty_Table_Is_Refused() => Assert.Contains("at least one", V([]));
    [Fact] public void The_First_Bracket_Must_Start_At_Zero() => Assert.Contains("start at 0", V([B(100, 5000), B(5000, null)]));

    [Fact] public void A_Gap_Is_Refused_With_A_Hint() =>
        Assert.Contains("join exactly", V([B(0, 4749.99m), B(4750, null)]));

    [Fact] public void An_Overlap_Is_Refused() => Assert.Contains("join exactly", V([B(0, 6000), B(5000, null)]));

    [Fact] public void The_Last_Bracket_Must_Be_Open_Ended() =>
        Assert.Contains("open-ended", V([B(0, 5000), B(5000, 10000)]));

    [Fact] public void Only_The_Last_Bracket_May_Be_Open_Ended() =>
        Assert.Contains("only the last", V([B(0, null), B(5000, null)]));

    [Fact] public void To_Must_Exceed_From() => Assert.Contains("greater than", V([B(0, 0)]));

    [Fact] public void Negative_Amounts_Are_Refused() => Assert.Contains("negative", V([B(0, null, fixedEe: -1)]));

    [Fact] public void A_Rate_Over_One_Is_Refused_With_A_Hint() =>
        Assert.Contains("between 0 and 1", V([B(0, null, rate: 15)]));

    // ---- kind and basis ----
    [Fact] public void A_Tax_Table_Needs_A_Pay_Basis() =>
        Assert.Contains("monthly or semi-monthly", V(Good, StatutoryKind.WithholdingTax));

    [Fact] public void A_Tax_Table_With_A_Basis_Is_Accepted() =>
        Assert.Null(V(Good, StatutoryKind.WithholdingTax, basis: PayFrequency.SemiMonthly));

    [Fact] public void Tax_Cannot_Be_A_Rate_Rule() =>
        Assert.Contains("bracket tables", V(null, StatutoryKind.WithholdingTax, StatutoryMethod.Rate, PayFrequency.Monthly, ee: 0.1m));

    [Fact] public void Only_Tax_Tables_Have_A_Basis() =>
        Assert.Contains("Only withholding", V(Good, basis: PayFrequency.Monthly));

    [Fact] public void The_Effective_Date_Must_Be_In_Range() =>
        Assert.Contains("out of range", V(Good, year: 1999));

    // ---- rate rules ----
    [Fact] public void A_Valid_Rate_Rule_Is_Accepted() =>
        Assert.Null(V(null, StatutoryKind.PhilHealth, StatutoryMethod.Rate, ee: 0.025m, er: 0.025m, floor: 10000, ceiling: 100000));

    [Fact] public void A_Rate_Rule_Cannot_Also_Have_Brackets() =>
        Assert.Contains("can't also", V(Good, StatutoryKind.PagIbig, StatutoryMethod.Rate, ee: 0.02m, er: 0.02m));

    [Fact] public void A_Percentage_Written_As_A_Whole_Number_Is_Refused() =>
        Assert.Contains("between 0 and 1", V(null, StatutoryKind.PagIbig, StatutoryMethod.Rate, ee: 2.5m, er: 2.5m));

    [Fact] public void Both_Rates_Zero_Is_Refused() =>
        Assert.Contains("At least one", V(null, StatutoryKind.PagIbig, StatutoryMethod.Rate));

    [Fact] public void A_Ceiling_Below_The_Floor_Is_Refused() =>
        Assert.Contains("ceiling", V(null, StatutoryKind.PagIbig, StatutoryMethod.Rate, ee: 0.02m, floor: 5000, ceiling: 1000));

    [Fact] public void A_Negative_Floor_Is_Refused() =>
        Assert.Contains("negative", V(null, StatutoryKind.PagIbig, StatutoryMethod.Rate, ee: 0.02m, floor: -1));
}