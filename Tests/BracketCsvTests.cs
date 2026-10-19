using Core.Common;

namespace Tests;

public class BracketCsvTests
{
    [Fact] public void A_Header_Row_Is_Skipped()
    {
        var r = BracketCsv.Parse("from,to,employee,employer,rate\n0,5000,100,200,0\n5000,,200,400,0");
        Assert.Null(r.Error);
        Assert.Equal(2, r.Rows.Count);
        Assert.Equal(new BracketRow(0m, 5000m, 100m, 200m, 0m), r.Rows[0]);
        Assert.Null(r.Rows[1].To);
    }

    [Fact] public void No_Header_Works_Too() =>
        Assert.Equal(1, BracketCsv.Parse("0,5000,100").Rows.Count);

    [Fact] public void A_Dash_Means_Open_Ended() =>
        Assert.Null(BracketCsv.Parse("10000,-,300").Rows[0].To);

    [Fact] public void Employer_And_Rate_Are_Optional() =>
        Assert.Equal(new BracketRow(0m, 5000m, 100m, 0m, 0m), BracketCsv.Parse("0,5000,100").Rows[0]);

    [Fact] public void Tab_Separated_Text_With_Thousands_Separators_Is_Understood()
    {
        var r = BracketCsv.Parse("0\t5,000\t100\t200\t0\n5,000\t\t1,200.50\t400\t0");
        Assert.Null(r.Error);
        Assert.Equal(5000m, r.Rows[0].To);
        Assert.Equal(5000m, r.Rows[1].From);
        Assert.Equal(1200.50m, r.Rows[1].EmployeeFixed);
    }

    [Fact] public void A_Percentage_Rate_Is_Converted()
    {
        var r = BracketCsv.Parse("10000\t30000\t0\t\t15%");
        Assert.Equal(0.15m, r.Rows[0].EmployeeRateOnExcess);
    }

    [Fact] public void A_Decimal_Rate_Is_Taken_As_Written() =>
        Assert.Equal(0.15m, BracketCsv.Parse("10000,30000,0,,0.15").Rows[0].EmployeeRateOnExcess);

    [Fact] public void Windows_Line_Endings_And_Blank_Lines_Are_Fine() =>
        Assert.Equal(2, BracketCsv.Parse("0,5000,100\r\n\r\n5000,,200\r\n").Rows.Count);

    [Fact] public void Errors_Name_The_Row_As_The_Person_Sees_It()
    {
        var r = BracketCsv.Parse("0,5000,100\n\n5000,abc,200");
        Assert.Contains("Row 3", r.Error);
        Assert.Contains("abc", r.Error);
    }

    [Fact] public void Too_Few_Columns_Is_Refused() => Assert.Contains("at least", BracketCsv.Parse("0,5000").Error);

    [Fact] public void A_Negative_Number_Is_Refused() => Assert.Contains("-5", BracketCsv.Parse("0,5000,-5").Error);

    [Fact] public void An_Ambiguous_Decimal_Comma_Is_Refused_Not_Misread() =>
        Assert.Contains("5,0", BracketCsv.Parse("0\t5,0\t1").Error);

    [Fact] public void Empty_Text_Is_Refused() => Assert.Contains("Paste", BracketCsv.Parse("  ").Error);

    [Fact] public void A_Header_Alone_Has_No_Rows() => Assert.Contains("No rows", BracketCsv.Parse("from,to,employee").Error);
}