using Core.Common;

namespace Tests;

public class WorkforceRulesTests
{
    private static DateOnly D(int y, int m, int d) => new(y, m, d);
    private static readonly DateOnly Today = D(2026, 10, 22);

    // ---- counting ----
    [Fact] public void Counts_Sort_By_Size_Then_Name()
    {
        var r = WorkforceRules.CountBy(["Sales", "IT", "IT", "HR", "Sales", "IT"]);
        Assert.Equal([new CountRow("IT", 3), new CountRow("Sales", 2), new CountRow("HR", 1)], r);
    }

    [Fact] public void Ties_Are_Alphabetical() =>
        Assert.Equal(["Admin", "Ops"], WorkforceRules.CountBy(["Ops", "Admin"]).Select(r => r.Label));

    [Fact] public void Blank_And_Null_Labels_Become_Unassigned()
    {
        var r = WorkforceRules.CountBy([null, "  ", "", "IT"]);
        Assert.Equal(3, r.Single(x => x.Label == "Unassigned").Count);
    }

    [Fact] public void Labels_Differing_Only_By_Surrounding_Space_Are_One_Group() =>
        Assert.Single(WorkforceRules.CountBy(["IT", " IT "]));

    [Fact] public void Nothing_Counts_To_Nothing() => Assert.Empty(WorkforceRules.CountBy([]));

    // ---- whole years ----
    [Theory]
    [InlineData(2020, 10, 22, 6)]    // anniversary today
    [InlineData(2020, 10, 23, 5)]    // anniversary tomorrow
    [InlineData(2020, 10, 21, 6)]
    [InlineData(2026, 10, 22, 0)]
    [InlineData(2026, 1, 1, 0)]
    [InlineData(2025, 10, 23, 0)]
    [InlineData(2025, 10, 22, 1)]
    public void Whole_Years_Count_Only_Completed_Years(int y, int m, int d, int expected) =>
        Assert.Equal(expected, WorkforceRules.WholeYears(D(y, m, d), Today));

    [Fact] public void A_Future_Hire_Has_Zero_Years() =>
        Assert.Equal(0, WorkforceRules.WholeYears(D(2027, 1, 1), Today));

    [Fact] public void A_Leap_Day_Hire_Completes_A_Year_On_March_1()
    {
        // hired Feb 29, 2020: on Feb 28, 2021 the year isn't complete, on Mar 1, 2021 it is
        Assert.Equal(0, WorkforceRules.WholeYears(D(2020, 2, 29), D(2021, 2, 28)));
        Assert.Equal(1, WorkforceRules.WholeYears(D(2020, 2, 29), D(2021, 3, 1)));
    }

    // ---- bands ----
    [Theory]
    [InlineData(2026, 5, 1, "Under 1 year")]
    [InlineData(2025, 10, 22, "1 to under 3 years")]
    [InlineData(2023, 10, 23, "1 to under 3 years")]
    [InlineData(2023, 10, 22, "3 to under 5 years")]
    [InlineData(2021, 10, 22, "5 to under 10 years")]
    [InlineData(2016, 10, 22, "10 years or more")]
    [InlineData(2000, 1, 1, "10 years or more")]
    public void Each_Hire_Lands_In_One_Band(int y, int m, int d, string band) =>
        Assert.Equal(band, WorkforceRules.TenureBand(D(y, m, d), Today));

    [Fact] public void The_Distribution_Lists_Every_Band_Even_When_Empty()
    {
        var r = WorkforceRules.TenureDistribution([D(2026, 6, 1), D(2026, 7, 1), D(2010, 1, 1)], Today);
        Assert.Equal(5, r.Count);
        Assert.Equal(2, r[0].Count);
        Assert.Equal(0, r[1].Count);
        Assert.Equal(1, r[4].Count);
        Assert.Equal(WorkforceRules.TenureBands, r.Select(x => x.Label));
    }

    // ---- next occurrence ----
    [Fact] public void A_Date_Later_This_Year_Stays_In_This_Year() =>
        Assert.Equal(D(2026, 12, 25), WorkforceRules.NextOccurrence(12, 25, Today));

    [Fact] public void Today_Counts_As_Today() =>
        Assert.Equal(Today, WorkforceRules.NextOccurrence(10, 22, Today));

    [Fact] public void A_Date_Already_Past_Rolls_To_Next_Year() =>
        Assert.Equal(D(2027, 3, 5), WorkforceRules.NextOccurrence(3, 5, Today));

    [Fact] public void Feb_29_Becomes_Feb_28_In_A_Non_Leap_Year() =>
        Assert.Equal(D(2027, 2, 28), WorkforceRules.NextOccurrence(2, 29, Today));

    [Fact] public void Feb_29_Is_Kept_In_A_Leap_Year() =>
        Assert.Equal(D(2028, 2, 29), WorkforceRules.NextOccurrence(2, 29, D(2028, 1, 10)));

    [Fact] public void A_Window_Includes_Both_Ends_And_Nothing_Beyond()
    {
        Assert.True(WorkforceRules.IsWithin(Today, Today, 30));
        Assert.True(WorkforceRules.IsWithin(Today.AddDays(30), Today, 30));
        Assert.False(WorkforceRules.IsWithin(Today.AddDays(31), Today, 30));
        Assert.False(WorkforceRules.IsWithin(Today.AddDays(-1), Today, 30));
    }

    // ---- CSV ----
    [Fact] public void Plain_Text_Is_Left_Alone() => Assert.Equal("IT", WorkforceRules.CsvCell("IT"));

    [Fact] public void Commas_And_Quotes_Are_Quoted() =>
        Assert.Equal("\"Smith, \"\"Jo\"\"\"", WorkforceRules.CsvCell("Smith, \"Jo\""));

    [Theory]
    [InlineData("=SUM(A1)", "'=SUM(A1)")]
    [InlineData("+1", "'+1")]
    [InlineData("-1", "'-1")]
    [InlineData("@cmd", "'@cmd")]
    public void Formula_Starters_Are_Defused(string input, string expected) =>
        Assert.Equal(expected, WorkforceRules.CsvCell(input));

    [Fact] public void A_Defused_Cell_With_A_Comma_Is_Also_Quoted() =>
        Assert.Equal("\"'=A,B\"", WorkforceRules.CsvCell("=A,B"));

    [Fact] public void Null_Is_An_Empty_Cell() => Assert.Equal("", WorkforceRules.CsvCell(null));

    [Fact] public void A_Csv_Has_A_Header_Then_One_Line_Per_Row()
    {
        var csv = WorkforceRules.ToCsv(["Department", "Count"], [["IT", "3"], ["HR", "1"]]);
        Assert.Equal("Department,Count\r\nIT,3\r\nHR,1\r\n".Replace("\r\n", Environment.NewLine), csv);
    }
}