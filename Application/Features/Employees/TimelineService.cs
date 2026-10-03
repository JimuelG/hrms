using Core.DTOs.Employees;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Employees;

public sealed class TimelineService(
    IGenericRepository<EmployeeTimelineEvent> repo) : ITimelineService
{
    public async Task<IReadOnlyList<TimelineEventDto>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct = default) =>
        (await repo.ListAsync(new TimelineByEmployeeSpecification(employeeId), ct)).Select(e => new TimelineEventDto(
            e.Id,
            e.EventType,
            e.Title,
            e.Description,
            e.EventDateUtc
        )).ToList();
    public void Record(Guid employeeId, TimeLineEventType type, string title, string? description = null, DateTime? eventDateUtc = null, Guid? recorededByUserId = null)
    {
        repo.Add(new EmployeeTimelineEvent
        {
           EmployeeId = employeeId,
           EventType = type,
           Title = title,
           Description = description,
           EventDateUtc = eventDateUtc ?? DateTime.UtcNow,
           RecordedByUserId = recorededByUserId 
        });
    }
}