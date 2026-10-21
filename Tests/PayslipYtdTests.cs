using Core.Common;
using Core.Entities;

namespace Tests;

public class PayslipYtdTests
{
    private static PayrollResultRecord Row(decimal gross, decimal taxable, decimal tax, decimal net, string? error = null) => new()
    {
        Id = Guid.NewGuid(), Gross = gross, TaxableIncome = taxable, Net = net, ErrorMessage = error,
        LinesJson = ResultSnapshot.Write(new List<PayrollLine> { new("TAX", "Withholding tax", tax, PayrollLineKind.Deduction) })
    };

    [Fact] public void Earlier_Results_And_The_Current_One_Are_Summed()
    {
        var ytd = PayslipYtd.Compute([Row(1000, 900, 50, 800), Row(2000, 1800, 150, 1600)], Row(500, 450, 25, 400));
        Assert.Equal(new YtdTotals(3500m, 3150m, 225m, 2800m), ytd);
    }

    [Fact] public void No_Earlier_Results_Gives_The_Current_Figures()
    {
        var ytd = PayslipYtd.Compute([], Row(1000, 900, 50, 800));
        Assert.Equal(new YtdTotals(1000m, 900m, 50m, 800m), ytd);
    }

    [Fact] public void The_Current_Row_Is_Never_Counted_Twice()
    {
        var current = Row(1000, 900, 50, 800);
        Assert.Equal(1000m, PayslipYtd.Compute([current], current).Gross);
    }

    [Fact] public void A_Row_With_An_Error_Adds_Nothing() =>
        Assert.Equal(1000m, PayslipYtd.Compute([Row(5000, 5000, 500, 4500, "No salary")], Row(1000, 900, 50, 800)).Gross);
}