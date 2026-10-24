using Core.Common;

namespace Tests;

public class OperationsRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 23, 2, 0, 0, DateTimeKind.Utc);

    [Fact] public void Zero_Days_Keeps_Everything() => Assert.Null(RetentionRules.Cutoff(Now, 0, 365));
    [Fact] public void Negative_Days_Keeps_Everything() => Assert.Null(RetentionRules.Cutoff(Now, -5, 365));

    [Fact] public void The_Default_Keeps_Three_Years() =>
        Assert.Equal(Now.AddDays(-1095), RetentionRules.Cutoff(Now, RetentionRules.DefaultAuditDays, RetentionRules.MinimumAuditDays));

    [Fact] public void A_Value_Below_The_Minimum_Is_Raised_To_It() =>
        Assert.Equal(Now.AddDays(-365), RetentionRules.Cutoff(Now, 10, 365));

    [Fact] public void A_Value_Above_The_Minimum_Is_Used_As_Is() =>
        Assert.Equal(Now.AddDays(-2000), RetentionRules.Cutoff(Now, 2000, 365));

    [Fact] public void A_Missing_Keys_Folder_Is_Missing() => Assert.Equal(KeysFolderState.Missing, KeysRules.State(false, 0));
    [Fact] public void An_Empty_Keys_Folder_Is_Empty() => Assert.Equal(KeysFolderState.Empty, KeysRules.State(true, 0));
    [Fact] public void A_Folder_With_Keys_Is_Healthy() => Assert.Equal(KeysFolderState.Healthy, KeysRules.State(true, 2));
}