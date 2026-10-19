using Core.Common;
using Core.DTOs.Payroll;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.VisualBasic;

namespace Application.Features.Payroll;

public sealed class StatutoryService(
    IUnitOfWork unit,
    IGenericRepository<StatutoryRuleSet> ruleSets,
    IGenericRepository<TenantSettings> settings,
    IUserDirectory users,
    ITenantContext tenant,
    TimeProvider clock) : IStatutoryService
{
    public async Task<ServiceResult<RuleSetDto>> CreateAsync(Guid actorUserId, CreateRuleSetDto dto, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(typeof(StatutoryKind), dto.Kind) || !Enum.IsDefined(typeof(StatutoryMethod), dto.Method))
            return ServiceResult<RuleSetDto>.Fail("Choose a valid kind and method.");

        if (dto.Basis is int rawBasis && !Enum.IsDefined(typeof(PayFrequency), rawBasis))
            return ServiceResult<RuleSetDto>.Fail("Choose a valid pay basis.");

        var kind = (StatutoryKind)dto.Kind;
        var method = (StatutoryMethod)dto.Method;
        PayFrequency? basis = dto.Basis is int b ? (PayFrequency)b : null;

        var source = dto.Source.Trim();
        if (source.Length == 0) return ServiceResult<RuleSetDto>.Fail("Say where these figures came from.");

        if (dto.Brackets is { Count: > 0 } && !string.IsNullOrWhiteSpace(dto.BracketsCsv))
            return ServiceResult<RuleSetDto>.Fail("Send the brackets as a list or as pasted text, not both.");

        IReadOnlyList<BracketRow> rows = [];
        if (!string.IsNullOrWhiteSpace(dto.BracketsCsv))
        {
            var parsed = BracketCsv.Parse(dto.BracketsCsv);
            if (parsed.Error is not null) return ServiceResult<RuleSetDto>.Fail(parsed.Error);
            rows = parsed.Rows;
        }
        else if (dto.Brackets is not null)
        {
            rows = dto.Brackets.Select(x => new BracketRow(x.From, x.To, x.EmployeeFixed, x.EmployerFixed, x.EmployeeRateOnExcess)).ToList();
        }

        var isBrackets = method == StatutoryMethod.Brackets;
        var employeeRate = isBrackets ? 0m : dto.EmployeeRate;
        var employerRate = isBrackets ? 0M : dto.EmployerRate;
        decimal? floor = isBrackets ? null : dto.BaseFloor;
        decimal? ceiling = isBrackets ? null : dto.BaseCeiling;

        var effective = dto.EffectiveFrom!.Value;
        var error = StatutoryRuleValidator.Validate(kind, method, basis, effective, employeeRate, employerRate, floor, ceiling, rows);
        if (error is not null) return ServiceResult<RuleSetDto>.Fail(error);

        if (await ruleSets.CountAsync(new StatutoryRuleSetDuplicationSpecification(kind, basis, effective), ct) > 0)
            return ServiceResult<RuleSetDto>.Fail(
                $"A table for this kind and basis already takes effect of {effective:yyyy-MM-dd}. Delete it first, or use a different date.",
                ServiceErrorType.Conflict);

        var entity = new StatutoryRuleSet
        {
            Kind = kind,
            Method = method,
            EffectiveFrom = effective,
            Basis = basis,
            EmployeeRate = employeeRate,
            EmployerRate = employerRate,
            BaseFloor = floor,
            BaseCeiling = ceiling,
            Source = source,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            CreatedByUserId = actorUserId
        };

        var sorted = rows.OrderBy(r => r.From).ToList();
        for (var i = 0; i < sorted.Count; i++)
        {
            entity.Brackets.Add(new StatutoryBracket
            {
                Order = i + 1,
                FromAmount = sorted[i].From,
                ToAmount = sorted[i].To,
                EmployeeFixed = sorted[i].EmployeeFixed,
                EmployerFixed = sorted[i].EmployerFixed,
                EmployeeRateOnExcess = sorted[i].EmployeeRateOnExcess
            }); 
        }

        ruleSets.Add(entity);
        await unit.Complete();

        return ServiceResult<RuleSetDto>.Success((await BuildAsync([entity], ct)).Single());
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var row = await ruleSets.GetByIdAsync(id, ct);
        if (row is null) return ServiceResult<bool>.Fail("Rule set not found.", ServiceErrorType.NotFound);

        ruleSets.Remove(row);
        await unit.Complete();
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<RuleSetDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var row = await ruleSets.GetEntityWithSpec(new StatutoryRuleSetByIdSpecificaiton(id), ct);
        if (row is null) return ServiceResult<RuleSetDto>.Fail("Rule set not found.", ServiceErrorType.NotFound);
        return ServiceResult<RuleSetDto>.Success((await BuildAsync([row], ct)).Single());
    }

    public async Task<IReadOnlyList<RuleSetDto>> ListAsync(StatutoryKind? kind, CancellationToken ct = default) =>
        await BuildAsync(await ruleSets.ListAsync(new StatutoryRuleSetsSpecification(kind), ct), ct);

    public async Task<InForceRules> LoadInForceAsync(DateOnly date, PayFrequency taxBasis, CancellationToken ct = default)
    {
        var all = await ruleSets.ListAsync(new StatutoryRuleSetsSpecification(null), ct);

        RuleSetData? Pick(StatutoryKind kind, PayFrequency? basis) => ToData(EffectiveDating.PickInForce(all.Where(r => r.Kind == kind && r.Basis == basis), r => r.EffectiveFrom, date));

        return new InForceRules(
            Pick(StatutoryKind.Sss, null),
            Pick(StatutoryKind.PhilHealth, null),
            Pick(StatutoryKind.PagIbig, null),
            Pick(StatutoryKind.WithholdingTax, taxBasis));
    }

    public async Task<ServiceResult<StatutoryPreviewDto>> PreviewAsync(StatutoryPreviewQuery q, CancellationToken ct = default)
    {
        var date = q.Date ?? await TodayAsync(ct);
        if (date.Year < LeaveCalculator.MinYear || date.Year > LeaveCalculator.MaxYear)
            return ServiceResult<StatutoryPreviewDto>.Fail($"Year must be between {LeaveCalculator.MinYear} and {LeaveCalculator.MaxYear}.");

        var taxBasis = PayFrequency.Monthly;
        if (q.TaxableIncome is not null)
        {
            if (q.TaxBasis is not int tb || !Enum.IsDefined(typeof(PayFrequency), tb))
                return ServiceResult<StatutoryPreviewDto>.Fail("Say wheter the taxable income is for monthly (2) or semi-monthly (1) pay.");
            taxBasis = (PayFrequency)tb;
        }

        var rules = await LoadInForceAsync(date, taxBasis, ct);
        var amount = q.MonthlyBase!.Value;

        var lines = new List<StatutoryLineDto>
        {
            Line("SSS", "SSS", rules.Sss, amount),
            Line("PHILHEALTH", "PhilHealth", rules.PhilHealth, amount),
            Line("PAGIBIG", "Pag-IBIG", rules.PagIbig, amount),
            q.TaxableIncome is decimal taxable
                ? Line("TAX", "Withholding tax", rules.Tax, taxable)
                : new StatutoryLineDto("TAX", "Withholding tax", 0m, 0m, null, "Not calculated: no taxable income was given.")
        };

        return ServiceResult<StatutoryPreviewDto>.Success(new StatutoryPreviewDto(date, lines));
    }

    private static StatutoryLineDto Line(string code, string label, RuleSetData? rs, decimal amount)
    {
        if (rs is null) return new(code, label, 0m, 0m, null, "No rule set is in force on this date.");
        var a = StatutoryCalculator.Compute(rs, amount);

        return new(code, label, a.Employee, a.Employer, rs.EffectiveFrom, null);
    }

    private async Task<DateOnly> TodayAsync(CancellationToken ct)
    {
        var s = await settings.GetEntityWithSpec(new TenantSettingsSpecification(), ct);
        return LocalClock.LocalDate(clock.GetUtcNow().UtcDateTime, s?.TimeZoneId);
    }

    private async Task<List<RuleSetDto>> BuildAsync(IReadOnlyList<StatutoryRuleSet> rows, CancellationToken ct)
    {
        var today = await TodayAsync(ct);

        var all = await ruleSets.ListAsync(new StatutoryRuleSetsSpecification(null), ct);
        var inForce = all.GroupBy(r => (r.Kind, r.Basis))
            .Select(g => EffectiveDating.PickInForce(g, r => r.EffectiveFrom, today))
            .Where(r => r is not null).Select(r => r!.Id).ToHashSet();

        var names = await users.GetDisplayNamesAsync(rows.Select(r => r.CreatedByUserId).Distinct().ToList(), tenant.RequiredTenantId, ct);

        return rows.Select(r => new RuleSetDto(
            r.Id,
            (int)r.Kind,
            (int)r.Method,
            r.EffectiveFrom,
            r.Basis is null ? null : (int)r.Basis,
            r.EmployeeRate,
            r.EmployerRate,
            r.BaseFloor,
            r.BaseCeiling,
            r.Source,
            r.Notes,
            names.GetValueOrDefault(r.CreatedByUserId, "Unknown"),
            r.CreatedAtUtc,
            inForce.Contains(r.Id),
            r.Brackets.OrderBy(b => b.Order)
                .Select(b => new BracketDto(b.Order, b.FromAmount, b.ToAmount, b.EmployeeFixed, b.EmployerFixed, b.EmployeeRateOnExcess)).ToList())).ToList();
    }

    private static RuleSetData? ToData(StatutoryRuleSet? r) => r is null ? null : new RuleSetData(
        r.Kind,
        r.Method,
        r.EffectiveFrom,
        r.Basis,
        r.EmployeeRate,
        r.EmployerRate,
        r.BaseFloor,
        r.BaseCeiling,
        r.Brackets.OrderBy(b => b.Order)
            .Select(b => new BracketRow(b.FromAmount, b.ToAmount, b.EmployeeFixed, b.EmployerFixed, b.EmployeeRateOnExcess))
            .ToList());
}