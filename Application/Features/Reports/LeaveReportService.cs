using Core.Common;
using Core.DTOs.Reports;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Reports;

public sealed class LeaveReportService(
    IGenericRepository<Employee> employees,
    IGenericRepository<LeaveRequest> requests,
    IGenericRepository<LeaveBalanceEntry> ledger,
    IGenericRepository<LeaveType> types) : ILeaveReportService
{
    public async Task<ServiceResult<LeaveCalendarDto>> CalendarAsync(DateOnly from, DateOnly to, Guid? departmentId, Guid? branchId, CancellationToken ct = default)
    {
        var error = ReportRangeRules.Check(from, to);
        if (error is not null) return ServiceResult<LeaveCalendarDto>.Fail(error);

        var reqs = await requests.ListAsync(new ApprovedLeaveSpecification(from, to), ct);
        var ids = reqs.Select(r => r.EmployeeId).Distinct().ToList();
        var people = (await employees.ListAsync(new EmployeesByIdsForPayrollSpecification(ids), ct))
            .Where(p => (departmentId == null || p.DepartmentId == departmentId) && (branchId == null || p.BranchId == branchId))
            .ToDictionary(p => p.Id);

        var away = reqs.Where(r => people.ContainsKey(r.EmployeeId))
            .Select(r => new AwayRange($"{people[r.EmployeeId].FirstName} {people[r.EmployeeId]}", r.StartDate, r.EndDate));

        return ServiceResult<LeaveCalendarDto>.Success(new LeaveCalendarDto(from, to, LeaveCalendarRules.Days(away, from, to)));
    }

    public async Task<ServiceResult<CsvFile>> ExportCalendarAsync(DateOnly from, DateOnly to, Guid? departmentId, Guid? branchId, CancellationToken ct = default)
    {
        var built = await CalendarAsync(from, to, departmentId, branchId, ct);
        if (!built.Succeeded) return ServiceResult<CsvFile>.Fail(built.Error!, built.ErrorType);

        var csv = WorkforceRules.ToCsv(["Date", "Name"],
            built.Value!.Days.SelectMany(d => d.Names.Select(n => new string?[] { d.Date.ToString("yyyy-MM-dd"), n })));
        return ServiceResult<CsvFile>.Success(new CsvFile($"leave-calendar-{from:yyyyMMdd}-{to:yyyyMMdd}.csv", csv));
    }

    public async Task<ServiceResult<CsvFile>> ExportUsageAsync(int year, Guid? departmentId, Guid? branchId, CancellationToken ct = default)
    {
        var built = await UsageAsync(year, departmentId, branchId, ct);
        if (!built.Succeeded) return ServiceResult<CsvFile>.Fail(built.Error!, built.ErrorType);

        var csv = WorkforceRules.ToCsv(
            ["Employee number", "Name", "Department", "Leave type code", "Leave type", "Approved days", "Pending days", "Balance"],
            built.Value!.Rows.Select(r => new string?[]
            {
                r.EmployeeNumber,
                r.Name,
                r.Department,
                r.LeaveTypeCode,
                r.LeaveTypeName,
                r.Approved.ToString("0.#"),
                r.Pending.ToString("0.#"),
                r.Balance?.ToString("0.#")
            }));
            
        return ServiceResult<CsvFile>.Success(new CsvFile($"leave-usage-{year}.csv", csv));
    }

    public async Task<ServiceResult<LeaveUsageReportDto>> UsageAsync(int year, Guid? departmentid, Guid? branchId, CancellationToken ct = default)
    {
        if (year < LeaveCalculator.MinYear || year > LeaveCalculator.MaxYear)
            return ServiceResult<LeaveUsageReportDto>.Fail("The year is out of range.");

        var reqs = await requests.ListAsync(new LeaveRequestsByYearSpecificaiton(year), ct);
        var entries = await ledger.ListAsync(new LeaveEntriesByYearSpecification(year), ct);
        var typeMap = (await types.ListAsync(new LeaveTypeSearchSpecification(null, activeOnly: false), ct)).ToDictionary(t => t.Id);

        var usage = LeaveUsageRules.Usage(reqs.Select(r => new LeaveRequestFact(r.EmployeeId, r.LeaveTypeId, r.Status, r.Days)));
        var keys = usage.Select(u => (u.EmployeeId, u.LeaveTypeId))
            .Union(entries.Select(e => (e.EmployeeId, e.LeaveTypeId))).ToList();

        var ids = keys.Select(k => k.EmployeeId).Distinct().ToList();
        var people = (await employees.ListAsync(new EmployeesByIdsForPayrollSpecification(ids), ct))
            .Where(p => (departmentid == null || p.DepartmentId == departmentid) && (branchId == null || p.BranchId == branchId))
            .ToDictionary(p => p.Id);

        var usageByKey = usage.ToDictionary(u => (u.EmployeeId, u.LeaveTypeId));
        var entriesByKey = entries.ToLookup(e => (e.EmployeeId, e.LeaveTypeId));

        var rows = new List<LeaveUsageRowDto>();
        foreach (var key in keys)
        {
            if (!people.TryGetValue(key.EmployeeId, out var person) || !typeMap.TryGetValue(key.LeaveTypeId, out var type)) continue;
            usageByKey.TryGetValue(key, out var u);

            decimal? balance = type.EntitlementMode == LeaveEntitlementMode.Unlimited
                ? null
                : LeaveBalanceMath.Summarize(entriesByKey[key].Select(e => new LedgerRow(e.Year, e.EntryType, e.Days, e.IsAutomatic))).Balance;
            
            rows.Add(new LeaveUsageRowDto(
                person.Id,
                person.EmployeeNumber,
                $"{person.FirstName} {person.LastName}",
                person.Department?.Name,
                type.Code,
                type.Name,
                u?.Approved ?? 0m,
                u?.Pending ?? 0m,
                balance));
        }

        var totals = LeaveUsageRules.TypeTotals(usage.Where(u => people.ContainsKey(u.EmployeeId)))
            .Where(t => typeMap.ContainsKey(t.LeaveTypeId))
            .Select(t => new LeaveTypeTotalDto(typeMap[t.LeaveTypeId].Code, typeMap[t.LeaveTypeId].Name, t.Approved, t.Pending))
            .OrderBy(t => t.Code).ToList();

        return ServiceResult<LeaveUsageReportDto>.Success(new LeaveUsageReportDto(
            year,
            rows.OrderBy(r => r.Name).ThenBy(r => r.LeaveTypeCode).ToList(),
            totals));
    }
}