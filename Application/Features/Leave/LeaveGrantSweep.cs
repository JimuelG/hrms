using Core.Common;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.Extensions.Logging;


namespace Application.Features.Leave;

public sealed class LeaveGrantSweep(
    IGenericRepository<Employee> employees,
    IGenericRepository<TenantSettings> tenantSettings,
    ILeaveBalanceService balances,
    ILogger<LeaveGrantSweep> log,
    TimeProvider clock) : ILeaveGrantSweep
{
    public async Task<int> EnsureAllAsync(CancellationToken ct = default)
    {
        var ts = await tenantSettings.GetEntityWithSpec(new TenantSettingsSpecification(), ct);
        var today = LocalClock.LocalDate(clock.GetUtcNow().UtcDateTime, ts?.TimeZoneId);

        var failed = 0;
        foreach (var person in await employees.ListAsync(new WorkingCandidatesSpecification(today, null), ct))
        {
            try
            {
                await balances.EnsureGrantsAsync(person, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                log.LogWarning(ex, "Leave grants failed for employee {EmployeeId}", person.Id);
            }
        }

        return failed;
    }
}