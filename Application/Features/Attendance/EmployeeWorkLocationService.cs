using Core.Common;
using Core.DTOs.Attendance;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Attendance;

public class EmployeeWorkLocationService(
    IUnitOfWork unit,
    IGenericRepository<EmployeeWorkLocation> assignments,
    IGenericRepository<WorkLocation> locations,
    IGenericRepository<Employee> employees,
    ITimelineService timeline) : IEmployeeWorkLocationService
{
    public async Task<IReadOnlyList<WorkLocationDto>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct = default)
    {
        var rows = await assignments.ListAsync(new AssignmentsByIdEmployeeSpecification(employeeId), ct);
        if (rows.Count == 0) return [];

        var ids = rows.Select(r => r.WorkLocationId).ToList();
        return (await locations.ListAsync(new WorkLocationsByIdsSpecification(ids), ct))
            .Select(WorkLocationService.ToDto).ToList();
    }

    public async Task<ServiceResult<IReadOnlyList<WorkLocationDto>>> SetForEmployeeAsync(Guid employeeId, IReadOnlyCollection<Guid> workLocationIds, CancellationToken ct = default)
    {
        if (await employees.GetByIdAsync(employeeId, ct) is null) 
            return ServiceResult<IReadOnlyList<WorkLocationDto>>.Fail("Employee not found.", ServiceErrorType.NotFound);

        var requested = workLocationIds.Distinct().ToList();

        IReadOnlyList<WorkLocation> found = [];

        if (requested.Count > 0)
        {
            found = await locations.ListAsync(new WorkLocationsByIdsSpecification(requested), ct);

            if (found.Count != requested.Count)
                return ServiceResult<IReadOnlyList<WorkLocationDto>>.Fail("One or more selected work locations were not found.");
            
            var inactive = found.FirstOrDefault(l => !l.IsActive);
            if (inactive is not null)
                return ServiceResult<IReadOnlyList<WorkLocationDto>>.Fail($"'{inactive.Name}' is inactive and can't be assigned.");
        }

        var current = await assignments.ListAsync(new AssignmentsByIdEmployeeSpecification(employeeId), ct);

        var toRemove = current.Where(a => !requested.Contains(a.WorkLocationId)).ToList();
        var toAdd = requested.Where(id => current.All(a => a.WorkLocationId != id)).ToList();

        assignments.RemoveRange(toRemove);
        foreach (var id in toAdd)
            assignments.Add(new EmployeeWorkLocation { EmployeeId = employeeId, WorkLocationId = id });

        if (toRemove.Count + toAdd.Count > 0)
        {
            var summary = found.Count == 0
                ? "No work locations assigned."
                : $"Now assigned to: {string.Join(", ", found.Select(l => l.Name))}.";
            timeline.Record(TimelineSubjectType.Employee, employeeId, TimeLineEventType.WorkLocationChanged, "Work locations updated", summary);
        }

        await unit.Complete();
        return ServiceResult<IReadOnlyList<WorkLocationDto>>.Success(found.Select(WorkLocationService.ToDto).ToList());
    }
}