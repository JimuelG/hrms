using Core.Entities;

namespace Core.Common;

public sealed record YtdTotals(decimal Gross, decimal TaxableIncome, decimal TaxWithheld, decimal Net);
public static class PayslipYtd
{
    public static decimal TaxWithheld(PayrollResultRecord r) =>
        ResultSnapshot.Read(r.LinesJson, new List<PayrollLine>()).Where(l => l.Code == "TAX").Sum(l => l.Amount);

    public static YtdTotals Compute(IEnumerable<PayrollResultRecord> earlier, PayrollResultRecord current)
    {
        var rows = earlier.Where(e => e.Id != current.Id).Append(current).Where(r => r.ErrorMessage is null).ToList();
        return new YtdTotals(rows.Sum(r => r.Gross), rows.Sum(r => r.TaxableIncome), rows.Sum(TaxWithheld), rows.Sum(r => r.Net));
    }
}