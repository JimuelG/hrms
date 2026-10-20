namespace Core.Common;
public static class PayrollComposer
{
    public static PayrollResult Compose(PayrollEmployeeInput input, PayrollOptions options, InForceRules rules)
    {
        var settings = options.Settings;
        var frequency = settings.Frequency;

        if (rules.Tax?.Basis is PayFrequency taxBasis && taxBasis != frequency)
            return Fail("The withholding tax table's pay basis doesn't match the pay frequency.");

        foreach (var a in input.Adjustments)
        {
            if (string.IsNullOrWhiteSpace(a.Label)) return Fail("Every adjusment needs a label.");
            if (a.Amount == 0m) return Fail($"The adjustment '{a.Label.Trim()}' has no amount.");
        }

        var gross = GrossPayCalculator.Calculate(input.Gross, settings);
        if (gross.Error is not null) return Fail(gross.Error);

        var notes = new List<string>(gross.Notes);
        var warnings = new List<string>(gross.Warnings);
        
        var lines = gross.Lines.Select(l => new PayrollLine(l.Code, l.Label, l.Amount, PayrollLineKind.Earning)).ToList();

        var firstHalf = input.Gross.Period.Start.Day <= 15;
        decimal employeeContributions = 0m, employerContributions = 0m;
        
        var window = PayPeriodCalculator.EmployedWindow(input.Gross.Period, input.Gross.HireDate, input.Gross.LastDay);
        var employedDays = window is null ? 0 : window.Value.End.DayNumber - window.Value.Start.DayNumber + 1;
        var fraction = decimal.Round((decimal)employedDays / input.Gross.Period.Days, 10);
        var factor = PartialPeriodRules.Factor(settings.PartialMonth, fraction);

        void Contribution(string code, string label, RuleSetData? rule)
        {
            if (rule is null)
            {
                warnings.Add($"No {label} table is in force for this period, so nothing was deducted.");
                return;
            }

            var monthly = StatutoryCalculator.Compute(rule, gross.MonthlyBasic);

            var employee = PartialPeriodRules.Apply(
                ContributionSpreader.ForPeriod(monthly.Employee, frequency, firstHalf, options.ContributionPolicy), factor);
            var employer = PartialPeriodRules.Apply( 
                ContributionSpreader.ForPeriod(monthly.Employer, frequency, firstHalf, options.ContributionPolicy), factor);

            employeeContributions += employee;
            employerContributions += employer;
            if (employee > 0m) lines.Add(new(code, label, employee, PayrollLineKind.Deduction));
        }

        Contribution("SSS", "SSS", rules.Sss);
        Contribution("PHILHEALTH", "PhilHealth", rules.PhilHealth);
        Contribution("PAGIBIG", "Pag-IBIG", rules.PagIbig);

        if (factor < 1m && (rules.Sss is not null || rules.PhilHealth is not null || rules.PagIbig is not null))
            notes.Add(settings.PartialMonth == PartialMonthContribution.SkipIfPartial
                ? "No contribution were taken, because the employee wasn't employed for the whole period."
                : $"Contributions were prorated to {fraction:P0}, matching they days employed.");

        var taxable = Math.Max(0m, gross.Gross - employeeContributions + AdjustmentRules.TaxableEffect(input.Adjustments));

        if (rules.Tax is null)
        {
            warnings.Add("No withholding tax table is in force for this period, so no tax was deducted.");
        }
        else
        {
            var tax = StatutoryCalculator.Compute(rules.Tax, taxable).Employee;
            if (tax > 0m) lines.Add(new("TAX", "Withholding tax", tax, PayrollLineKind.Deduction));
        }

        foreach (var a in input.Adjustments)
        {
            var amount = Money(a.Amount);
            var label = a.Label.Trim();
            lines.Add(amount > 0m
                ? new PayrollLine("ADJUSTMENT", label, amount, PayrollLineKind.Earning)
                : new PayrollLine("ADJUSTMENT", label, -amount, PayrollLineKind.Deduction));
        }

        if (input.Adjustments.Any(a => a.Tax == AdjustmentTax.Taxable))
            notes.Add("Taxable adjustments were included in taxable income.");
        else if (input.Adjustments.Count > 0)
            notes.Add("Manual adjustments don't change taxable income.");

        var earnings = lines.Where(l => l.Kind == PayrollLineKind.Earning).Sum(l => l.Amount);
        var deductions = lines.Where(l => l.Kind == PayrollLineKind.Deduction).Sum(l => l.Amount);
        var net = earnings - deductions;
        if (net < 0m) warnings.Add("Net pay is negative. Review the deductions and adjustments before finalizing.");

        return new PayrollResult(
            lines,
            gross.Gross,
            taxable,
            earnings,
            deductions,
            net,
            employerContributions,
            gross.MonthlyBasic,
            gross.DailyRate,
            gross.HourlyRate,
            notes,
            warnings,
            null);
    }

    private static decimal Money(decimal v) => decimal.Round(v, 2, MidpointRounding.AwayFromZero);

    private static PayrollResult Fail(string error) => new([], 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, [], [], error);

    public static PayrollResult Failure(string error) => Fail(error);
}