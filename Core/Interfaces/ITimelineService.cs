using Core.DTOs.Employees;
using Core.Enums;

namespace Core.Interfaces;
public interface ITimelineService
{
    void Record(
        Guid employeeId,
        TimeLineEventType type,
        string title,
        string? description = null,
        DateTime? eventDateUtc = null,
        Guid? recorededByUserId = null);

    Task<IReadOnlyList<TimelineEventDto>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct = default);
}