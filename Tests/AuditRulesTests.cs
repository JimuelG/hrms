using Core.Common;

namespace Tests;

public class AuditRulesTests
{
    [Theory]
    [InlineData("ProposedSalary")]
    [InlineData("PasswordHash")]
    [InlineData("SecurityStamp")]
    [InlineData("TokenHash")]
    [InlineData("TinProtected")]
    [InlineData("BankAccountNumberProtected")]
    [InlineData("ClientSecret")]
    public void Secret_Looking_Fields_Are_Sensitive(string field) => Assert.True(AuditRules.IsSensitiveField(field));

    [Theory]
    [InlineData("Name")]
    [InlineData("Code")]
    [InlineData("Status")]
    [InlineData("EmployeeRate")]
    [InlineData("HireDate")]
    public void Ordinary_Fields_Are_Not_Sensitive(string field) => Assert.False(AuditRules.IsSensitiveField(field));

    [Fact] public void Salary_And_Id_Entities_Are_Hidden_Whatever_The_Case()
    {
        Assert.True(AuditRules.IsHiddenEntity("EmployeeCompensation"));
        Assert.True(AuditRules.IsHiddenEntity("employeeidentifiers"));
        Assert.False(AuditRules.IsHiddenEntity("Branch"));
    }

    [Fact] public void An_Added_Row_Shows_Every_Field_Alphabetically()
    {
        var c = AuditRules.Changes("Branch", null, """{"Name":"Main","Code":"HQ"}""");
        Assert.Equal(["Code", "Name"], c.Select(x => x.Field));
        Assert.All(c, x => Assert.Null(x.Old));
        Assert.Equal("HQ", c[0].New);
    }

    [Fact] public void A_Modified_Row_Shows_Only_What_Changed()
    {
        var c = AuditRules.Changes("Branch", """{"Name":"Main","Code":"HQ"}""", """{"Name":"Head office","Code":"HQ"}""");
        var change = Assert.Single(c);
        Assert.Equal(("Name", "Main", "Head office"), (change.Field, change.Old, change.New));
    }

    [Fact] public void A_Deleted_Row_Shows_What_Was_Removed()
    {
        var c = AuditRules.Changes("Branch", """{"Name":"Main"}""", null);
        Assert.Equal(("Name", "Main", (string?)null), (c[0].Field, c[0].Old, c[0].New));
    }

    [Fact] public void A_Sensitive_Field_Shows_That_It_Changed_But_Never_The_Value()
    {
        var c = AuditRules.Changes("JobOffer", """{"ProposedSalary":40000,"Status":1}""", """{"ProposedSalary":45000,"Status":1}""");
        var change = Assert.Single(c);
        Assert.True(change.Hidden);
        Assert.Equal((AuditRules.HiddenValue, AuditRules.HiddenValue), (change.Old, change.New));
    }

    [Fact] public void A_Hidden_Entity_Shows_No_Changes() =>
        Assert.Empty(AuditRules.Changes("EmployeeCompensation", null, """{"MonthlyBasicSalary":30000}"""));

    [Fact] public void Row_Version_Is_Left_Out() =>
        Assert.DoesNotContain(AuditRules.Changes("Branch", null, """{"Name":"A","RowVersion":"AAAA"}"""), x => x.Field == "RowVersion");

    [Fact] public void Numbers_And_Booleans_Are_Shown_As_Written()
    {
        var c = AuditRules.Changes("Branch", null, """{"IsActive":true,"Count":5}""");
        Assert.Equal(["5", "true"], c.Select(x => x.New));
    }

    [Fact] public void A_Value_Cleared_To_Null_Is_A_Change()
    {
        var change = Assert.Single(AuditRules.Changes("Branch", """{"City":"Tarlac"}""", """{"City":null}"""));
        Assert.Equal(("Tarlac", (string?)null), (change.Old, change.New));
    }

    [Fact] public void Damaged_Json_Gives_No_Changes_Instead_Of_Throwing() =>
        Assert.Empty(AuditRules.Changes("Branch", "{ not json", "also not json"));

    [Fact] public void Page_Size_Is_Bounded()
    {
        Assert.Equal((1, 50), AuditRules.ClampPage(0, 0));
        Assert.Equal((3, 100), AuditRules.ClampPage(3, 500));
    }

    [Fact] public void Ranges_Are_Checked()
    {
        Assert.Null(AuditRules.CheckRange(null, null));
        Assert.Contains("before the start", AuditRules.CheckRange(new DateOnly(2026, 5, 1), new DateOnly(2026, 4, 1)));
        Assert.Contains("at most 366", AuditRules.CheckRange(new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 2)));
    }

    private enum Sample { Draft = 1, Sent = 2 }
    [Flags] private enum Days { None = 0, Mon = 1, Tue = 2 }

    [Fact] public void An_Enum_Number_Shows_Its_Name() => Assert.Equal("Sent", AuditRules.FormatValue(typeof(Sample), "2"));
    [Fact] public void A_Nullable_Enum_Works_Too() => Assert.Equal("Draft", AuditRules.FormatValue(typeof(Sample?), "1"));
    [Fact] public void An_Undefined_Number_Stays_A_Number() => Assert.Equal("99", AuditRules.FormatValue(typeof(Sample), "99"));
    [Fact] public void A_Plain_Number_Is_Untouched() => Assert.Equal("8", AuditRules.FormatValue(typeof(int), "8"));
    [Fact] public void A_Value_Already_Stored_As_A_Name_Is_Untouched() => Assert.Equal("Sent", AuditRules.FormatValue(typeof(Sample), "Sent"));
    [Fact] public void Null_Stays_Null() => Assert.Null(AuditRules.FormatValue(typeof(Sample), null));
    [Fact] public void Flags_Show_Combined_Names() => Assert.Equal("Mon, Tue", AuditRules.FormatValue(typeof(Days), "3"));

    [Fact] public void Changes_Use_The_Field_Type_When_Given()
    {
        var change = Assert.Single(AuditRules.Changes("Application", """{"Status":1}""", """{"Status":2}""",
            f => f == "Status" ? typeof(Sample) : null));
        Assert.Equal(("Draft", "Sent"), (change.Old, change.New));
    }

    [Fact] public void A_Sensitive_Enum_Field_Is_Still_Hidden()
    {
        var change = Assert.Single(AuditRules.Changes("JobOffer", """{"SalaryBand":1}""", """{"SalaryBand":2}""",
            _ => typeof(Sample)));
        Assert.Equal((AuditRules.HiddenValue, AuditRules.HiddenValue), (change.Old, change.New));
    }
}