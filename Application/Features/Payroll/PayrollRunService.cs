using System.Data;
using Core.Common;
using Core.DTOs.Payroll;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Payroll;

public class PayrollRunService(
    IUnitOfWork unit,
    IGenericRepository<PayrollRun> runs,
    IGenericRepository<PayrollResultRecord> results,
    IGenericRepository<PayrollSettingsRecord> settingsRepo,
    IGenericRepository<Employee> employees,
    IGenericRepository<EmployeeCompensation> compensation,
    IGenericRepository<WorkSchedule> schedules,
    IGenericRepository<TenantSettings> tenantSettings,
    IGenericRepository<LeaveRequest> leaveRequests,
    IGenericRepository<LeaveType> leaveTypes,
    IGenericRepository<OvertimeRequest> overtime,
    IGenericRepository<PayrollAdjustmentRecord> adjustmentRepo,
    ILeaveCalculationService leaveCalculation,
    IStatutoryService statutory,
    TimeProvider clock) : IPayrollRunService
{
    public async Task<ServiceResult<PayrollRunDetailDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var run = await runs.GetByIdAsync(id, ct);
        if (run is null) return ServiceResult<PayrollRunDetailDto>.Fail("Payroll run not found.", ServiceErrorType.NotFound);

        return ServiceResult<PayrollRunDetailDto>.Success(await DetailAsync(run, ct));
    }

    public async Task<IReadOnlyList<PayrollRunSummaryDto>> ListAsync(CancellationToken ct = default)
    {
        var all = await runs.ListAsync(new PayrollRunsSpecification(), ct);
        var summaries = new List<PayrollRunSummaryDto>();
        foreach (var run in all)
            summaries.Add(Summarize(run, await results.ListAsync(new PayrollResultsByRunSpecification(run.Id), ct)));
        return summaries;
    }

    public async Task<ServiceResult<PayrollRunDetailDto>> RecomputeAsync(Guid actorUserId, Guid id, CancellationToken ct = default)
    {
        var run = await runs.GetEntityWithSpec(new PayrollRunByIdSpecification(id), ct);
        if (run is null) return ServiceResult<PayrollRunDetailDto>.Fail("Payroll run not found.", ServiceErrorType.NotFound);

        if (!PayrollRunRules.IsEditable(run.Status))
            return ServiceResult<PayrollRunDetailDto>.Fail("Only a draft run can be recomputed.", ServiceErrorType.Conflict);

        results.RemoveRange(await results.ListAsync(new PayrollResultsByRunSpecification(run.Id), ct));
        await ComputeIntoAsync(run, ct);

        run.LastActivityAtUtc = clock.GetUtcNow().UtcDateTime;
        await unit.Complete();

        return ServiceResult<PayrollRunDetailDto>.Success(await DetailAsync(run, ct));
    }

    public async Task<ServiceResult<PayrollRunDetailDto>> StartAsync(Guid actorUserId, StartRunDto dto, CancellationToken ct = default)
    {
        var settings = await GetOrCreateSettingsAsync(ct);
        var period = new PayPeriod(dto.PeriodStart!.Value, dto.PeriodEnd!.Value);
        
        var today = await TodayAsync(ct);
        var error = PayrollRunRules.CheckPeriod(period, settings.Frequency, today);
        if (error is not null) return ServiceResult<PayrollRunDetailDto>.Fail(error);

        if (await runs.CountAsync(new PayrollRunByPeriodSpecification(period.Start, period.End), ct) > 0)
            return ServiceResult<PayrollRunDetailDto>.Fail("A payroll run already exists for this period. Cancel it first if you need to start over.", ServiceErrorType.Conflict);

        var run = new PayrollRun
        {
            PeriodStart = period.Start,
            PeriodEnd = period.End,
            Status = PayrollRunStatus.Draft,
            Frequency = settings.Frequency,
            ContributionPolicy = settings.ContributionPolicy,
            WorkingDaysFactorFiveDay = settings.WorkingDaysFactorFiveDay,
            WorkingDaysFactorySixDay = settings.WorkingDaysFactorSixDay,
            HoursPerDay = settings.HoursPerDay,
            OrdinaryOvertimeMultiplier = settings.OrdinaryOvertimeMultiplier,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            CreatedByUserId = actorUserId,
            LastActivityAtUtc = clock.GetUtcNow().UtcDateTime
        };

        runs.Add(run);

        await ComputeIntoAsync(run, ct);
        await unit.Complete();

        return ServiceResult<PayrollRunDetailDto>.Success(await DetailAsync(run, ct));
    }

    private async Task ComputeIntoAsync(PayrollRun run, CancellationToken ct)
    {
        var settings = run.ToSettings();
        var period = run.Period;
        var options = new PayrollOptions(settings, run.ContributionPolicy);

        var people = await employees.ListAsync(new EmployeesForPayrollSpecification(period.End), ct);
        var ids = people.Select(p => p.Id).ToList();

        var history = (await compensation.ListAsync(new CompensationForEmployeesSpecification(ids), ct))
            .GroupBy(c => c.EmployeeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SalaryPoint>)g.Select(c => new SalaryPoint(c.EffectiveFrom, c.MonthlyBasicSalary)).ToList());

        var rules = await statutory.LoadInForceAsync(period.End, run.Frequency, ct);

        var adjustments = (await runs.GetEntityWithSpec(new PayrollRunByIdSpecification(run.Id, withAdjustments: true), ct))?.Adjustments ?? run.Adjustments;

        var adjustmentsByEmployee = adjustments.GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PayrollAdjustment>)g.Select(a => new PayrollAdjustment(a.Label, a.Amount)).ToList());

        var unpaidLeave = await leaveRequests.ListAsync(new ApprovedUnpaidLeaveSpecification(period.Start, period.End), ct);
        var types = (await leaveTypes.ListAsync(new LeaveTypeSearchSpecification(null, activeOnly: false), ct)).ToDictionary(t => t.Id);
        var otMinutes = (await overtime.ListAsync(new ApprovedOvertimeInRangeSpecification(period.Start, period.End), ct))
            .GroupBy(o => o.EmployeeId).ToDictionary(g => g.Key, g => g.Sum(o => o.ApprovedMinutes ?? 0));

        var companyDays = (await tenantSettings.GetEntityWithSpec(new TenantSettingsSpecification(), ct))?.WorkingDays ?? new TenantSettings().WorkingDays;

        foreach (var person in people)
        {
            decimal unpaidDays = 0m;
            foreach (var req in unpaidLeave.Where(r => r.EmployeeId == person.Id))
            {
                if (!types.TryGetValue(req.LeaveTypeId, out var type)) continue;
                var calc = await leaveCalculation.CalculateAsync(person, type, req.StartDate, req.EndDate, req.StartPortion, req.EndPortion, ct);
                if (calc.Succeeded) unpaidDays += LeaveInPeriod.Days(calc.Value!.Lines, period);
            }

            var workingDays = companyDays;
            if (person.ScheduleId is Guid sid)
            {
                var schedule = await schedules.GetByIdAsync(sid, ct);
                if (schedule is not null) workingDays = schedule.WorkingDays;
            }

            var perWeek = LeaveInPeriod.WorkingDaysPerWeek(workingDays);

            history.TryGetValue(person.Id, out var personHistory);
            var minutes = otMinutes.GetValueOrDefault(person.Id);

            var gross = new GrossInput(personHistory ?? [], person.HireDate, null, period, perWeek, unpaidDays, minutes);

            var composed = PayrollComposer.Compose(new PayrollEmployeeInput(gross, adjustmentsByEmployee.GetValueOrDefault(person.Id) ?? []), options, rules);

            results.Add(ToRecord(run, person, composed, gross));
        }
    }

    private static PayrollResultRecord ToRecord(PayrollRun run, Employee person, PayrollResult r, GrossInput input)
    {
        var record = new PayrollResultRecord
        {
            PayrollRunId = run.Id,
            EmployeeId = person.Id,
            EmployeeNumber = person.EmployeeNumber,
            EmployeeName = $"{person.FirstName} {person.LastName}",
            DepartmentName = person.Department?.Name,
            PositionTitle = person.Position?.Title
        };
        record.Run = run;

        if (r.Error is not null)
        {
            record.ErrorMessage = r.Error;
            return record;
        }

        record.MonthlyBasic = r.MonthlyBasic;
        record.Gross = r.Gross;
        record.TaxableIncome = r.TaxableIncome;
        record.TotalEarnings = r.TotalEarnings;
        record.TotalDeductions = r.TotalDeductions;
        record.Net = r.Net;
        record.EmployerContributions = r.EmployerContributions;
        record.LinesJson = ResultSnapshot.Write(r.Lines);
        record.NotesJson = ResultSnapshot.Write(r.Notes);
        record.WarningsJson = ResultSnapshot.Write(r.Warnings);
        record.InputsJson = ResultSnapshot.Write(new ResultSnapshot.InputsSnapshot(
            input.UnpaindLeaveDays,
            input.ApprovedOvertimeMinutes,
            input.WorkingDaysPerWeek,
            input.HireDate,
            input.LastDay,
            r.DailyRate,
            r.HourlyRate));

        return record;
    }

    private async Task<PayrollSettingsRecord> GetOrCreateSettingsAsync(CancellationToken ct)
    {
        var existing = await settingsRepo.GetEntityWithSpec(new PayrollSettingsSpecification(), ct);
        if (existing is not null) return existing;

        var created = new PayrollSettingsRecord();
        settingsRepo.Add(created);
        try
        {
            await unit.Complete();
        }
        catch (DuplicateKeyException)
        {
            return (await settingsRepo.GetEntityWithSpec(new PayrollSettingsSpecification(), ct))!;
        }

        return created;
    }

    private async Task<DateOnly> TodayAsync(CancellationToken ct)
    {
        var s = await tenantSettings.GetEntityWithSpec(new TenantSettingsSpecification(), ct);
        return LocalClock.LocalDate(clock.GetUtcNow().UtcDateTime, s?.TimeZoneId);
    }

    private async Task<PayrollRunDetailDto> DetailAsync(PayrollRun run, CancellationToken ct)
    {
        var rows = await results.ListAsync(new PayrollResultsByRunSpecification(run.Id), ct);
        return new PayrollRunDetailDto(Summarize(run, rows), rows.Select(ToDto).ToList());
    }

    private static PayrollRunSummaryDto Summarize(PayrollRun run, IReadOnlyList<PayrollResultRecord> rows) => new(
        run.Id,
        run.PeriodStart,
        run.PeriodEnd,
        (int)run.Status,
        rows.Count,
        rows.Count(r => r.ErrorMessage is null),
        rows.Sum(r => r.Gross),
        rows.Sum(r => r.TotalDeductions),
        rows.Sum(r => r.Net),
        rows.Sum(r => r.EmployerContributions),
        run.CreatedAtUtc,
        run.FinalizedAtUtc);

    private static PayrollResultDto ToDto(PayrollResultRecord r) => new(
        r.Id,
        r.EmployeeId,
        r.EmployeeNumber,
        r.EmployeeName,
        r.DepartmentName,
        r.PositionTitle,
        r.MonthlyBasic,
        r.Gross,
        r.TaxableIncome,
        r.TotalEarnings,
        r.TotalDeductions,
        r.Net,
        r.EmployerContributions,
        ResultSnapshot.Read(r.LinesJson, new List<PayrollLine>()).Select(l => new PayrollLineDto(l.Code, l.Label, l.Amount, (int)l.Kind)).ToList(),
        ResultSnapshot.Read(r.NotesJson, new List<string>()),
        ResultSnapshot.Read(r.WarningsJson, new List<string>()),
        r.ErrorMessage);

    public async Task<ServiceResult<IReadOnlyList<PayrollAdjustmentDto>>> GetAdjusmentsAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await runs.GetEntityWithSpec(new PayrollRunByIdSpecification(runId, withAdjustments: true), ct);
        if (run is null) return ServiceResult<IReadOnlyList<PayrollAdjustmentDto>>.Fail("Payroll run not found.", ServiceErrorType.NotFound);
        return ServiceResult<IReadOnlyList<PayrollAdjustmentDto>>.Success(
            run.Adjustments.Select(a => new PayrollAdjustmentDto(a.Id, a.EmployeeId, a.Label, a.Amount)).ToList());
    }

    public async Task<ServiceResult<PayrollRunDetailDto>> AddAdjustmentAsync(Guid actorUserId, Guid runId, AddAdjusmentDto dto, CancellationToken ct = default)
    {
        var run = await runs.GetEntityWithSpec(new PayrollRunByIdSpecification(runId), ct);
        if (run is null) return Fail("Payroll run not found.", ServiceErrorType.NotFound);
        if (!PayrollRunRules.IsEditable(run.Status)) return Fail("Only a draft run can be changed.", ServiceErrorType.Conflict);

        var error = PayrollRunRules.CheckAdjustment(dto.Label, dto.Amount);
        if (error is not null) return Fail(error);

        var inRun = (await results.ListAsync(new PayrollResultsByRunSpecification(runId), ct)).Any(r => r.EmployeeId == dto.EmployeeId);
        if (!inRun) return Fail("That employee isn't part of this run.");

        adjustmentRepo.Add(new PayrollAdjustmentRecord
        {
           PayrollRunId = runId,
           EmployeeId = dto.EmployeeId!.Value,
           Label = dto.Label.Trim(),
           Amount = dto.Amount,
           CreatedByUserId = actorUserId 
        });

        await unit.Complete();

        return await RecomputeAsync(actorUserId, runId, ct);
    }

    public async Task<ServiceResult<PayrollRunDetailDto>> RemoveAdjustmentAsync(Guid actorUserId, Guid runId, Guid adjustmentId, CancellationToken ct = default)
    {
        var run = await runs.GetEntityWithSpec(new PayrollRunByIdSpecification(runId), ct);
        if (run is null) return Fail("Payroll run not found.", ServiceErrorType.NotFound);
        if (!PayrollRunRules.IsEditable(run.Status)) return Fail("Only a draft run can be changed.", ServiceErrorType.Conflict);

        var adjustment = await adjustmentRepo.GetEntityWithSpec(new PayrollAdjustmentByIdSpecification(adjustmentId, runId), ct);
        if (adjustment is null) return Fail("Adjust not found.", ServiceErrorType.NotFound);

        adjustmentRepo.Remove(adjustment);
        await unit.Complete();

        return await RecomputeAsync(actorUserId, runId, ct);
    }

    public async Task<ServiceResult<PayrollRunDetailDto>> ApproveAsync(Guid actorUserId, Guid runId, CancellationToken ct = default)
    {
        var run = await runs.GetEntityWithSpec(new PayrollRunByIdSpecification(runId), ct);
        if (run is null) return Fail("Payroll run not found.", ServiceErrorType.NotFound);

        var rows = await results.ListAsync(new PayrollResultsByRunSpecification(runId), ct);
        var error = PayrollRunRules.CheckApprove(run.Status, run.CreatedByUserId, actorUserId, rows.Count(r => r.ErrorMessage is not null));
        if (error is not null) return Fail(error);

        var now = clock.GetUtcNow().UtcDateTime;
        run.Status = PayrollRunStatus.Approved;
        run.ApprovedByUserId = actorUserId;
        run.ApprovedAtUtc = now;
        run.LastActivityAtUtc = now;
        await unit.Complete();

        return ServiceResult<PayrollRunDetailDto>.Success(await DetailAsync(run, ct));
    }

    public async Task<ServiceResult<PayrollRunDetailDto>> SendBackAsync(Guid actorUserId, Guid runId, string? reason, CancellationToken ct = default)
    {
        var run = await runs.GetEntityWithSpec(new PayrollRunByIdSpecification(runId), ct);
        if (run is null) return Fail("Payroll run not found.", ServiceErrorType.NotFound);
        if (!PayrollRunRules.CanMove(run.Status, PayrollRunStatus.Draft)) return Fail("Only an approved run can be sent back.", ServiceErrorType.Conflict);
        if (string.IsNullOrWhiteSpace(reason)) return Fail("Say why it is being send back.");

        run.Status = PayrollRunStatus.Draft;
        run.ApprovedByUserId = null;
        run.ApprovedAtUtc = null;
        run.Notes = AppendNote(run.Notes, $"Send back: {reason.Trim()}");
        run.LastActivityAtUtc = clock.GetUtcNow().UtcDateTime;
        await unit.Complete();

        return ServiceResult<PayrollRunDetailDto>.Success(await DetailAsync(run, ct));
    }

    public async Task<ServiceResult<PayrollRunDetailDto>> FinalizeAsync(Guid actorUserId, Guid runId, CancellationToken ct = default)
    {
        var run = await runs.GetEntityWithSpec(new PayrollRunByIdSpecification(runId), ct);
        if (run is null) return Fail("Payroll run not found.", ServiceErrorType.NotFound);

        var error = PayrollRunRules.CheckFinalize(run.Status, run.ApprovedByUserId, actorUserId);
        if (error is not null) return Fail(error);

        var now = clock.GetUtcNow().UtcDateTime;
        run.Status = PayrollRunStatus.Finalized;
        run.FinalizedByUserId = actorUserId;
        run.FinalizedAtUtc = now;
        run.LastActivityAtUtc = now;
        await unit.Complete();

        return ServiceResult<PayrollRunDetailDto>.Success(await DetailAsync(run, ct));
    }

    public async Task<ServiceResult<PayrollRunDetailDto>> CancelAsync(Guid actorUserId, Guid runId, string? reason, CancellationToken ct = default)
    {
        var run = await runs.GetEntityWithSpec(new PayrollRunByIdSpecification(runId), ct);
        if (run is null) return Fail("Payroll run not found", ServiceErrorType.NotFound);
        if (!PayrollRunRules.CanMove(run.Status, PayrollRunStatus.Cancelled))
            return Fail("Only a draft run can be cancelled. Send an approved run back first.", ServiceErrorType.Conflict);
        if (string.IsNullOrWhiteSpace(reason)) return Fail("Say why it is being cancelled.");

        var now = clock.GetUtcNow().UtcDateTime;
        run.Status = PayrollRunStatus.Cancelled;
        run.CancelledByUserId = actorUserId;
        run.CancelledAtUtc = now;
        run.CancellationReason = reason.Trim();
        run.LastActivityAtUtc = now;
        await unit.Complete();

        return ServiceResult<PayrollRunDetailDto>.Success(await DetailAsync(run, ct));
    }

    public async Task<PayrollSettingsDto> GetSettingsAsync(CancellationToken ct = default) => 
        ToDto(await GetOrCreateSettingsAsync(ct));

    public async Task<ServiceResult<PayrollSettingsDto>> UpdateSettingsAsync(UpdatePayrollSettingsDto dto, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(typeof(PayFrequency), dto.Frequency) || !Enum.IsDefined(typeof(ContributionDeduction), dto.ContributionPolicy))
            return ServiceResult<PayrollSettingsDto>.Fail("Choose a valid frequency and contribution policy.");

        var settings = await GetOrCreateSettingsAsync(ct);
        settings.Frequency = (PayFrequency)dto.Frequency;
        settings.ContributionPolicy = (ContributionDeduction)dto.ContributionPolicy;
        settings.WorkingDaysFactorFiveDay = dto.WorkingDaysFactorFiveDay;
        settings.WorkingDaysFactorSixDay = dto.WorkingDaysFactorSixDay;
        settings.HoursPerDay = dto.HoursPerDay;
        settings.OrdinaryOvertimeMultiplier = dto.OrdinaryOvertimeMultiplier;
        await unit.Complete();

        return ServiceResult<PayrollSettingsDto>.Success(ToDto(settings));
    }

    private static PayrollSettingsDto ToDto(PayrollSettingsRecord s) => new(
        (int)s.Frequency,
        (int)s.ContributionPolicy,
        s.WorkingDaysFactorFiveDay,
        s.WorkingDaysFactorSixDay,
        s.HoursPerDay,
        s.OrdinaryOvertimeMultiplier);

    private static string AppendNote(string? existing, string line) =>
        string.IsNullOrEmpty(existing) ? line : $"{existing}\n{line}";


    private static ServiceResult<PayrollRunDetailDto> Fail(string message, ServiceErrorType type = ServiceErrorType.Validation) =>
        ServiceResult<PayrollRunDetailDto>.Fail(message, type);
}