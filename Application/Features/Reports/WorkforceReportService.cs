using Core.Common;
using Core.DTOs.Reports;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Reports;

public class WorkforceReportService(
    IGenericRepository<Employee> employees,
    IGenericRepository<TenantSettings> tenantSettings,
    TimeProvider clock) : IWorkforceReportService
{
    private const int WindowDays = 30;
    private static readonly string[] Breakdowns = ["department", "branch", "position", "type", "status", "tenure", "birthdays", "anniversaries"];
    public async Task<ServiceResult<CsvFile>> ExportAsync(string breakdown, CancellationToken ct = default)
    {
        var key = (breakdown ?? "").Trim().ToLowerInvariant();
        if (!Breakdowns.Contains(key))
            return ServiceResult<CsvFile>.Fail($"Choose one of: {string.Join(", ", Breakdowns)}.");

        var (today, people) = await LoadAsync(ct);

        string[] headers;
        IEnumerable<string?[]> rows;

        switch (key)
        {
            case "department": (headers, rows) = Counts("Department", WorkforceRules.CountBy(people.Select(p => p.Department?.Name)));
                break;
            case "branch": (headers, rows) = Counts("Branch", WorkforceRules.CountBy(people.Select(p => p.Branch?.Name)));
                break;
            case "position" : (headers, rows) = Counts("Position", WorkforceRules.CountBy(people.Select(p => p.Position?.Title)));
                break;
            case "type": (headers, rows) = Counts("Employment Type", WorkforceRules.CountBy(people.Select(p => p.EmploymentType.ToString())));
                break;
            case "status": (headers, rows) = Counts("Status", WorkforceRules.CountBy(people.Select(p => p.Status.ToString())));
                break;
            case "tenure": (headers, rows) = Counts("Tenure", WorkforceRules.TenureDistribution(people.Select(p => p.HireDate), today));
                break;
            case "birthdays":
                headers = ["Name", "Month", "Day", "Next"];
                rows = Birthdays(people, today, all: true).Select(c => new string?[] { c.Name, c.Month.ToString(), c.Day.ToString(), c.Next.ToString("yyyy-MM-dd") });
                break;
            default:
                headers = ["Name", "Month", "Day", "Next", "Years"];
                rows = Anniversaries(people, today, all: true).Select(c => new string?[] { c.Name, c.Month.ToString(), c.Day.ToString(), c.Next.ToString("yyyy-MM-dd"), c.Years?.ToString() });
                break;
        }

        return ServiceResult<CsvFile>.Success(new CsvFile($"workforce-{key}-{today:yyyyMMdd}.csv", WorkforceRules.ToCsv(headers, rows)));
    }

    public async Task<ServiceResult<WorkforceReportDto>> GetAsync(CancellationToken ct = default)
    {
        var (today, people) = await LoadAsync(ct);

        return ServiceResult<WorkforceReportDto>.Success(new WorkforceReportDto(
            today,
            people.Count,
            WorkforceRules.CountBy(people.Select(p => p.Department?.Name)),
            WorkforceRules.CountBy(people.Select(p => p.Branch?.Name)),
            WorkforceRules.CountBy(people.Select(p => p.Position?.Title)),
            WorkforceRules.CountBy(people.Select(p => p.EmploymentType.ToString())),
            WorkforceRules.CountBy(people.Select(p => p.Status.ToString())),
            WorkforceRules.TenureDistribution(people.Select(p => p.HireDate), today),
            Birthdays(people, today),
            Anniversaries(people, today)));
    }

    private async Task<(DateOnly Today, List<Employee> People)> LoadAsync(CancellationToken ct)
    {
        var ts = await tenantSettings.GetEntityWithSpec(new TenantSettingsSpecification(), ct);
        var today = LocalClock.LocalDate(clock.GetUtcNow().UtcDateTime, ts?.TimeZoneId);

        var people = (await employees.ListAsync(new WorkforceEmployeesSpecification(today), ct))
            .Where(p => HeadcountRules.IsWorking(p.Status, p.HireDate, p.LastWorkingDay, today)).ToList();
        return (today, people);
    }

    private static (string[] Headers, IEnumerable<string?[]> Rows) Counts(string label, IEnumerable<CountRow> rows) => 
        ([label, "Count"], rows.Select(r => new string?[] {r.Label, r.Count.ToString() }));

    private static List<CelebrationDto> Birthdays(List<Employee> people, DateOnly today, bool all = false) => people
        .Where(p => p.DateOfBirth is not null)
        .Select(p => (p, d: p.DateOfBirth!.Value, next: WorkforceRules.NextOccurrence(p.DateOfBirth!.Value.Month, p.DateOfBirth.Value.Day, today)))
        .Where(x => all || WorkforceRules.IsWithin(x.next, today, WindowDays))
        .OrderBy(x => x.next).ThenBy(x => x.p.LastName)
        .Select(x => new CelebrationDto($"{x.p.FirstName} {x.p.LastName}", x.d.Month, x.d.Day, x.next, null))
        .ToList();

    private static List<CelebrationDto> Anniversaries(List<Employee> people, DateOnly today, bool all = false) => people
        .Select(p => (p, next: WorkforceRules.NextOccurrence(p.HireDate.Month, p.HireDate.Day, today)))
        .Select(x => (x.p, x.next, years: x.next.Year - x.p.HireDate.Year))
        .Where(x => x.years >= 1 && (all || WorkforceRules.IsWithin(x.next, today, WindowDays)))
        .OrderBy(x => x.next).ThenBy(x => x.p.LastName)
        .Select(x => new CelebrationDto($"{x.p.FirstName} {x.p.LastName}", x.p.HireDate.Month, x.p.HireDate.Day, x.next, x.years))
        .ToList();
}