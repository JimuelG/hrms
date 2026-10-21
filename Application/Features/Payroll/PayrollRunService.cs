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
    IGenericRepository<Holiday> holidays,
    IGenericRepository<AttendanceRecord> attendance,
    IGenericRepository<RecurringPayrollItem> recurring,
    IGenericRepository<PayrollLoanApplication> loanApplications,
    IGenericRepository<LoanRepayment> loanRepayments,
    ILeaveCalculationService leaveCalculation,
    IStatutoryService statutory,
    ITimeZoneResolver zones,
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
        loanApplications.RemoveRange(await loanApplications.ListAsync(new LoanApplicationsByRunSpecification(run.Id), ct));

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
            WorkingDaysFactorSixDay = settings.WorkingDaysFactorSixDay,
            HoursPerDay = settings.HoursPerDay,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            OrdinaryOvertimeMultiplier = settings.OrdinaryOvertimeMultiplier,
            RestDayOvertimeMultiplier = settings.RestDayOvertimeMultiplier,
            HolidayOvertimeMultiplier = settings.HolidayOvertimeMultiplier,
            CreatedByUserId = actorUserId,
            LastActivityAtUtc = clock.GetUtcNow().UtcDateTime,
            AttendanceDeductionsEnabled = settings.AttendanceDeductionsEnabled,
            AttendanceGraceMinutes = settings.AttendanceGraceMinutes,
            PartialMonthContribution = settings.PartialMonthContribution,
            TaxMethod = settings.TaxMethod,
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

        var people = await employees.ListAsync(new EmployeesForPayrollSpecification(period.Start, period.End), ct);
        var ids = people.Select(p => p.Id).ToList();

        var history = (await compensation.ListAsync(new CompensationForEmployeesSpecification(ids), ct))
            .GroupBy(c => c.EmployeeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SalaryPoint>)g.Select(c => new SalaryPoint(c.EffectiveFrom, c.MonthlyBasicSalary)).ToList());

        var taxBasis = settings.TaxMethod == TaxMethod.Cumulative ? PayFrequency.Monthly : run.Frequency;
        var rules = await statutory.LoadInForceAsync(period.End, taxBasis, ct);

        var adjustments = (await runs.GetEntityWithSpec(new PayrollRunByIdSpecification(run.Id, withAdjustments: true), ct))?.Adjustments ?? run.Adjustments;

        var adjustmentsByEmployee = adjustments.GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PayrollAdjustment>)g.Select(a => new PayrollAdjustment(a.Label, a.Amount, a.Tax)).ToList());

        var unpaidLeave = await leaveRequests.ListAsync(new ApprovedUnpaidLeaveSpecification(period.Start, period.End), ct);
        var types = (await leaveTypes.ListAsync(new LeaveTypeSearchSpecification(null, activeOnly: false), ct)).ToDictionary(t => t.Id);

        var holidayDates = (await holidays.ListAsync(new HolidaysInRangeSpecification(period.Start, period.End), ct)).Select(h => h.Date).ToHashSet();

        var overtimeRows = (await overtime.ListAsync(new ApprovedOvertimeInRangeSpecification(period.Start, period.End), ct))
            .GroupBy(o => o.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var attendanceRows = (await attendance.ListAsync(new AttendanceInPeriodSpecification(period.Start, period.End), ct))
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(a => a.Date));

        var leaveByEmployee = settings.AttendanceDeductionsEnabled
            ? (await leaveRequests.ListAsync(new ApprovedLeaveSpecification(period.Start, period.End), ct))
                .GroupBy(r => r.EmployeeId).ToDictionary(g => g.Key, g => g.ToList())
            : new Dictionary<Guid, List<LeaveRequest>>();

        var rulesByEmployee = (await recurring.ListAsync(new ActiveRecurringItemsInPeriodSpecification(period.Start, period.End), ct))
            .GroupBy(r => r.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        var companyDays = (await tenantSettings.GetEntityWithSpec(new TenantSettingsSpecification(), ct))?.WorkingDays ?? new TenantSettings().WorkingDays;

        var ytdByEmployee = new Dictionary<Guid, YearToDate>();
        if (settings.TaxMethod == TaxMethod.Cumulative)
        {
            var earlier = await results.ListAsync(new FinalizedResultsInYearSpecification(period.End.Year, period.Start), ct);

            ytdByEmployee = earlier
                .GroupBy(r => r.EmployeeId)
                .ToDictionary(g => g.Key, g => new YearToDate(
                    g.Sum(r => r.TaxableIncome),
                    g.Sum(r => ResultSnapshot.Read(r.LinesJson, new List<PayrollLine>())
                        .Where(l => l.Code == "TAX").Sum(l => l.Amount))));
        }

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

            var zone = ResolveZone(await zones.ResolveAsync(person, ct));
            var theirAttendance = attendanceRows.GetValueOrDefault(person.Id);

            var items = new List<(OvertimeDayType, int)>();
            var checks = new List<string>();
            foreach (var ot in overtimeRows.GetValueOrDefault(person.Id) ?? [])
            {
                items.Add((OvertimeClassifier.Classify(ot.Date, workingDays, holidayDates), ot.ApprovedMinutes ?? 0));

                AttendanceRecord? rec = null;
                var has = theirAttendance?.TryGetValue(ot.Date, out rec) == true;
                TimeOnly? outLocal = has && rec!.ClockOutUtc is DateTime o
                    ? TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(o, DateTimeKind.Utc), zone))
                    : null;

                var flag = OvertimeAttendanceCheck.Check(ot.Date, ot.EndTime, has, outLocal);
                if (flag is not null) checks.Add(flag);
            }

            AttendanceDeductionInput? attendanceInput = null;
            if (settings.AttendanceDeductionsEnabled)
            {
                var window = PayPeriodCalculator.EmployedWindow(period, person.HireDate, person.LastWorkingDay);
                var days = theirAttendance ?? new Dictionary<DateOnly, AttendanceRecord>();
                var flags = new List<string>();
                int late = 0, under = 0;

                foreach (var rec in days.Values.Where(r => window is not null && r.Date >= window.Value.Start && r.Date <= window.Value.End))
                {
                    if (rec.ScheduledStart is not TimeOnly start || rec.ScheduledEnd is not TimeOnly end) continue;

                    late += AttendanceDeductionRules.LateMinutes(start, TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(rec.ClockInUtc, DateTimeKind.Utc), zone)),
                    settings.AttendanceGraceMinutes);

                    if (rec.ClockOutUtc is DateTime outUtc)
                        under += AttendanceDeductionRules.UndertimeMinutes(end, TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(outUtc, DateTimeKind.Utc), zone)),
                        settings.AttendanceGraceMinutes);
                    else
                        flags.Add($"{rec.Date:MMM d} has no clock-out, so undertime wasn't deducted for that day.");
                }

                var leaveDates = window is null
                    ? new HashSet<DateOnly>()
                    : await LeaveDatesAsync(person, leaveByEmployee.GetValueOrDefault(person.Id) ?? [], types, period, ct);

                var absent = window is null ? 0 : AttendanceDeductionRules.AbsentDates(window.Value.Start, window.Value.End, workingDays, days.Keys.ToHashSet(), leaveDates, holidayDates).Count;

                attendanceInput = new AttendanceDeductionInput(true, settings.AttendanceGraceMinutes, absent, late, under, flags);
            }

            var personAdjustments = new List<PayrollAdjustment>(adjustmentsByEmployee.GetValueOrDefault(person.Id) ?? []);
            var loansApplied = new List<PayrollLoanApplication>();

            foreach (var rule in rulesByEmployee.GetValueOrDefault(person.Id) ?? [])
            {
                if (!RecurringRules.Applies(rule.From, rule.To, rule.IsActive, period)) continue;

                var amount = rule.Amount;
                if (rule.TotalOwed is decimal owed)
                {
                    var instalment = RecurringRules.LoanInstalment(rule.Amount, new LoanState(owed, rule.PaidSoFar));
                    if (instalment <= 0m) continue;
                    amount = -instalment;
                    loansApplied.Add(new PayrollLoanApplication
                    {
                        PayrollRunId = run.Id,
                        RecurringPayrollItemId = rule.Id,
                        Amount = instalment
                    });
                }

                personAdjustments.Add(new PayrollAdjustment(rule.Label, amount, rule.Tax));
            }

            var gross = new GrossInput(personHistory ?? [], person.HireDate, person.LastWorkingDay, period, perWeek, unpaidDays, OvertimeClassifier.Bucket(items), attendanceInput);

            var separatedWithoutDate = person.LastWorkingDay is null
                && (person.Status is EmployeeStatus.Resigned or EmployeeStatus.Terminated);

            var composed = separatedWithoutDate
                ? PayrollComposer.Failure("This employee is marked as separated but has no last working day. Set it on their profile.")
                : PayrollComposer.Compose(new PayrollEmployeeInput(gross, personAdjustments, ytdByEmployee.GetValueOrDefault(person.Id)), options, rules);

            if (composed.Error is null && checks.Count > 0)
                composed = composed with { Warnings = composed.Warnings.Concat(checks).ToList() };

            if (composed.Error is null)
                foreach (var a in loansApplied) loanApplications.Add(a);

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
            input.UnpaidLeaveDays,
            input.Overtime.Sum(b => b.Minutes),
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
        rows.Count(r => r.ErrorMessage is not null),
        rows.Sum(r => r.Gross),
        rows.Sum(r => r.TotalDeductions),
        rows.Sum(r => r.Net),
        rows.Sum(r => r.EmployerContributions),
        run.CreatedAtUtc,
        run.FinalizedAtUtc,
        (int)run.Kind);

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

    public async Task<ServiceResult<IReadOnlyList<PayrollAdjustmentDto>>> GetAdjustmentsAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await runs.GetEntityWithSpec(new PayrollRunByIdSpecification(runId, withAdjustments: true), ct);
        if (run is null) return ServiceResult<IReadOnlyList<PayrollAdjustmentDto>>.Fail("Payroll run not found.", ServiceErrorType.NotFound);
        return ServiceResult<IReadOnlyList<PayrollAdjustmentDto>>.Success(
            run.Adjustments.Select(a => new PayrollAdjustmentDto(a.Id, a.EmployeeId, a.Label, a.Amount, (int)a.Tax)).ToList());
    }

    public async Task<ServiceResult<PayrollRunDetailDto>> AddAdjustmentAsync(Guid actorUserId, Guid runId, AddAdjustmentDto dto, CancellationToken ct = default)
    {
        var run = await runs.GetEntityWithSpec(new PayrollRunByIdSpecification(runId), ct);
        if (run is null) return Fail("Payroll run not found.", ServiceErrorType.NotFound);
        if (!PayrollRunRules.IsEditable(run.Status)) return Fail("Only a draft run can be changed.", ServiceErrorType.Conflict);

        var error = PayrollRunRules.CheckAdjustment(dto.Label, dto.Amount);
        if (error is not null) return Fail(error);

        var inRun = (await results.ListAsync(new PayrollResultsByRunSpecification(runId), ct)).Any(r => r.EmployeeId == dto.EmployeeId);
        if (!inRun) return Fail("That employee isn't part of this run.");

        if (!Enum.IsDefined(typeof(AdjustmentTax), dto.Tax)) return Fail("Choose a valid tax treatment.");

        adjustmentRepo.Add(new PayrollAdjustmentRecord
        {
           PayrollRunId = runId,
           EmployeeId = dto.EmployeeId!.Value,
           Label = dto.Label.Trim(),
           Amount = dto.Amount,
           Tax = (AdjustmentTax)dto.Tax,
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
        if (string.IsNullOrWhiteSpace(reason)) return Fail("Say why it is being sent back.");

        run.Status = PayrollRunStatus.Draft;
        run.ApprovedByUserId = null;
        run.ApprovedAtUtc = null;
        run.Notes = AppendNote(run.Notes, $"Sent back: {reason.Trim()}");
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

        var applications = await loanApplications.ListAsync(new LoanApplicationsByRunSpecification(runId), ct);
        var loans = applications.Count == 0
            ? new Dictionary<Guid, RecurringPayrollItem>()
            : (await recurring.ListAsync(new RecurringItemsByIdsSpecification(
                applications.Select(a => a.RecurringPayrollItemId).ToList()), ct)).ToDictionary(i => i.Id);

        foreach (var app in applications)
        {
            if (!loans.TryGetValue(app.RecurringPayrollItemId, out var item) || item.TotalOwed is null)
                return Fail("A recurring loan used by this run no longer exists. Recompute the run.", ServiceErrorType.Conflict);

            var problem = RecurringRules.CheckFinalizeApplication(item.Label, item.TotalOwed.Value, item.PaidSoFar, app.Amount);
            if (problem is not null) return Fail(problem, ServiceErrorType.Conflict);
        }

        foreach (var app in applications)
        {
            loans[app.RecurringPayrollItemId].PaidSoFar += app.Amount;
            loanRepayments.Add(new LoanRepayment
            {
                RecurringPayrollItemId = app.RecurringPayrollItemId,
                PayrollRunId = runId,
                Amount = app.Amount
            });
        }

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

        if (!Enum.IsDefined(typeof(PartialMonthContribution), dto.PartialMonthContribution))
            return ServiceResult<PayrollSettingsDto>.Fail("Choose a valid rule for the partial months.");

        if (!Enum.IsDefined(typeof(TaxMethod), dto.TaxMethod))
            return ServiceResult<PayrollSettingsDto>.Fail("Choose a valid tax method.");

        if ((TaxMethod)dto.TaxMethod == TaxMethod.Cumulative && (PayFrequency)dto.Frequency != PayFrequency.Monthly)
            return ServiceResult<PayrollSettingsDto>.Fail("Cumulative tax needs monthly pay in this version. Set the pay frequency to Monthly, or use per-period tax.");

        var settings = await GetOrCreateSettingsAsync(ct);
        settings.Frequency = (PayFrequency)dto.Frequency;
        settings.ContributionPolicy = (ContributionDeduction)dto.ContributionPolicy;
        settings.WorkingDaysFactorFiveDay = dto.WorkingDaysFactorFiveDay;
        settings.WorkingDaysFactorSixDay = dto.WorkingDaysFactorSixDay;
        settings.HoursPerDay = dto.HoursPerDay;
        settings.OrdinaryOvertimeMultiplier = dto.OrdinaryOvertimeMultiplier;
        settings.RestDayOvertimeMultiplier = dto.RestDayOvertimeMultiplier;
        settings.HolidayOvertimeMultiplier = dto.HolidayOvertimeMultiplier;
        settings.AttendanceDeductionsEnabled = dto.AttendanceDeductionsEnabled;
        settings.AttendanceGraceMinutes = dto.AttendanceGraceMinutes;
        settings.PartialMonthContribution = (PartialMonthContribution)dto.PartialMonthContribution;
        settings.TaxMethod = (TaxMethod)dto.TaxMethod;
        settings.ThirteenthMonthExemptCeiling = dto.ThirteenthMonthExemptCeiling;
        await unit.Complete();

        return ServiceResult<PayrollSettingsDto>.Success(ToDto(settings));
    }

    public async Task<ServiceResult<PayrollRunDetailDto>> StartThirteenthAsync(Guid actorUserId, StartThirteenthDto dto, CancellationToken ct = default)
    {
        var settings = await GetOrCreateSettingsAsync(ct);
        var cutoff = dto.CutoffDate ?? new DateOnly(dto.Year, 12, 31);

        var error = ThirteenthMonthRules.CheckPeriod(dto.Year, cutoff, await TodayAsync(ct));
        if (error is not null) return Fail(error);

        var existing = (await runs.ListAsync(new ThirteenthRunsInYearSpecification(dto.Year), ct))
            .Select(r => (r.PeriodEnd, r.Status));
        error = ThirteenthMonthRules.CheckExisting(existing, cutoff);
        if (error is not null) return Fail(error, ServiceErrorType.Conflict);

        var start = new DateOnly(dto.Year, 1, 1);
        if (await results.CountAsync(new FinalizedRegularResultsThroughSpecification(start, cutoff), ct) == 0)
            return Fail($"No finalized payroll runs were found for {dto.Year} up to {cutoff:yyyy-MM-dd}. Finalize the regular runs first.");

        var run = new PayrollRun
        {
            Kind = PayrollRunKind.ThirteenthMonth,
            PeriodStart = start, 
            PeriodEnd = cutoff, 
            Status = PayrollRunStatus.Draft,
            Frequency = settings.Frequency,
            ContributionPolicy = settings.ContributionPolicy,
            WorkingDaysFactorFiveDay = settings.WorkingDaysFactorFiveDay,
            WorkingDaysFactorSixDay = settings.WorkingDaysFactorSixDay,
            HoursPerDay = settings.HoursPerDay,
            OrdinaryOvertimeMultiplier = settings.OrdinaryOvertimeMultiplier,
            RestDayOvertimeMultiplier = settings.RestDayOvertimeMultiplier, 
            HolidayOvertimeMultiplier = settings.HolidayOvertimeMultiplier,
            AttendanceDeductionsEnabled = settings.AttendanceDeductionsEnabled, 
            AttendanceGraceMinutes = settings.AttendanceGraceMinutes,
            PartialMonthContribution = settings.PartialMonthContribution, 
            TaxMethod = settings.TaxMethod,
            ThirteenthMonthExemptCeiling = settings.ThirteenthMonthExemptCeiling,
            CreatedByUserId = actorUserId, LastActivityAtUtc = clock.GetUtcNow().UtcDateTime
        };
        runs.Add(run);

        await ComputeThirteenthAsync(run, ct);
        await unit.Complete();

        return ServiceResult<PayrollRunDetailDto>.Success(await DetailAsync(run, ct));
    }

    private static PayrollSettingsDto ToDto(PayrollSettingsRecord s) => new(
        (int)s.Frequency,
        (int)s.ContributionPolicy,
        s.WorkingDaysFactorFiveDay,
        s.WorkingDaysFactorSixDay,
        s.HoursPerDay,
        s.OrdinaryOvertimeMultiplier,
        s.RestDayOvertimeMultiplier,
        s.HolidayOvertimeMultiplier,
        s.AttendanceDeductionsEnabled,
        s.AttendanceGraceMinutes,
        (int)s.PartialMonthContribution,
        (int)s.TaxMethod,
        s.ThirteenthMonthExemptCeiling);

    private static string AppendNote(string? existing, string line) =>
        string.IsNullOrEmpty(existing) ? line : $"{existing}\n{line}";


    private static ServiceResult<PayrollRunDetailDto> Fail(string message, ServiceErrorType type = ServiceErrorType.Validation) =>
        ServiceResult<PayrollRunDetailDto>.Fail(message, type);

    private static TimeZoneInfo ResolveZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return TimeZoneInfo.Utc;
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        } catch (Exception)
        {
            return TimeZoneInfo.Utc;
        }
    }

    private async Task<HashSet<DateOnly>> LeaveDatesAsync(
        Employee person, IEnumerable<LeaveRequest> requests, IReadOnlyDictionary<Guid, LeaveType> types, PayPeriod period, CancellationToken ct)
    {
        var dates = new HashSet<DateOnly>();
        foreach (var req in requests)
        {
            if (!types.TryGetValue(req.LeaveTypeId, out var type)) continue;
            var calc = await leaveCalculation.CalculateAsync(person, type, req.StartDate, req.EndDate, req.StartPortion, req.EndPortion, ct);
            if (!calc.Succeeded) continue;

            foreach (var line in calc.Value!.Lines)
                if (line.Status == LeaveDayStatus.Counted && line.Date >= period.Start && line.Date <= period.End)
                    dates.Add(line.Date);
        }

        return dates;
    }

    private async Task ComputeThirteenthAsync(PayrollRun run, CancellationToken ct)
    {
        var cutoff = run.PeriodEnd;
        var year = cutoff.Year;

        var regular = await results.ListAsync(new FinalizedRegularResultsThroughSpecification(run.PeriodStart, cutoff), ct);
        var earnedBy = regular.GroupBy(r => r.EmployeeId).ToDictionary(g => g.Key,
            g => g.Sum(r => ThirteenthMonthRules.BasicEarned(ResultSnapshot.Read(r.LinesJson, new List<PayrollLine>()))));

        var prior = await results.ListAsync(new FinalizedThirteenthResultsSpecification(year, cutoff), ct);
        var paidBy = prior.GroupBy(r => r.EmployeeId).ToDictionary(g => g.Key,
                g => (Paid: g.Sum(r => r.Gross), Exempt: g.Sum(r => r.Gross - r.TaxableIncome)));

        var ytdBy = new Dictionary<Guid, YearToDate>();
        RuleSetData? taxRule = null;
        if (run.TaxMethod == TaxMethod.Cumulative)
        {
            taxRule = (await statutory.LoadInForceAsync(cutoff, PayFrequency.Monthly, ct)).Tax;
            var earlier = await results.ListAsync(new FinalizedResultsInYearSpecification(year, cutoff.AddDays(1)), ct);
            ytdBy = earlier.GroupBy(r => r.EmployeeId).ToDictionary(g => g.Key, g => new YearToDate(
                g.Sum(r =>  r.TaxableIncome),
                g.Sum(r => ResultSnapshot.Read(r.LinesJson, new List<PayrollLine>()).Where(l => l.Code == "TAX").Sum(l => l.Amount))));
        }

        var adjustments = (await runs.GetEntityWithSpec(new PayrollRunByIdSpecification(run.Id, withAdjustments: true), ct))?.Adjustments
                            ?? run.Adjustments;

        var adjustmentsBy = adjustments.GroupBy(a => a.EmployeeId).ToDictionary(g => g.Key,
            g => (IReadOnlyList<PayrollAdjustment>)g.Select(a => new PayrollAdjustment(a.Label, a.Amount, a.Tax)).ToList());

        var ids = earnedBy.Keys.Union(paidBy.Keys).ToList();
        foreach (var person in await employees.ListAsync(new EmployeesByIdsForPayrollSpecification(ids), ct))
        {
            var earned = earnedBy.GetValueOrDefault(person.Id);
            var (paid, exemptUsed) = paidBy.GetValueOrDefault(person.Id);
            if (earned == 0m && paid == 0m) continue;

            var amount = ThirteenthMonthCalculator.Calculate(new(earned, paid, exemptUsed, run.ThirteenthMonthExemptCeiling));
            var composed = ThirteenthMonthComposer.Compose(amount, adjustmentsBy.GetValueOrDefault(person.Id) ?? [], run.TaxMethod, taxRule, ytdBy.GetValueOrDefault(person.Id));

            results.Add(ToThirteenthRecord(run, person, composed, new ThirteenthInputsSnapshot(earned, paid, amount.Entitlement)));
        }
    }

    private static PayrollResultRecord ToThirteenthRecord(PayrollRun run, Employee person, PayrollResult r, ThirteenthInputsSnapshot inputs)
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

        record.Gross = r.Gross;              
        record.TaxableIncome = r.TaxableIncome;
        record.TotalEarnings = r.TotalEarnings;
        record.TotalDeductions = r.TotalDeductions;
        record.Net = r.Net;
        record.LinesJson = ResultSnapshot.Write(r.Lines);
        record.NotesJson = ResultSnapshot.Write(r.Notes);
        record.WarningsJson = ResultSnapshot.Write(r.Warnings);
        record.InputsJson = ResultSnapshot.Write(inputs);
        return record;
    }
}