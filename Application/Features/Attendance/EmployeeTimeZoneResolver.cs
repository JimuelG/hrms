using Core.Entities;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Attendance;

public class EmployeeTimeZoneResolver(
    IGenericRepository<Branch> branches,
    IGenericRepository<TenantSettings> settings) : ITimeZoneResolver
{
    public async Task<string?> ResolveAsync(Employee employee, CancellationToken ct = default)
    {
        var branch = await branches.GetByIdAsync(employee.BranchId, ct);
        if (!string.IsNullOrWhiteSpace(branch?.TimeZoneId)) return branch!.TimeZoneId;

        var s = await settings.GetEntityWithSpec(new TenantSettingsSpecification(), ct);
        return s?.TimeZoneId;
    }
}