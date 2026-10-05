using Core.DTOs.Employees;
using Core.Enums;

namespace Core.Interfaces;
public interface ITimelineService
{
    void Record(
        TimelineSubjectType subjectType,
        Guid subjectId,
        TimeLineEventType type,
        string title,
        string? description = null,
        DateTime? eventDateUtc = null,
        Guid? recorededByUserId = null);

    Task<IReadOnlyList<TimelineEventDto>> GetForEmployeeAsync(TimelineSubjectType subjectType, Guid subjectId, CancellationToken ct = default);
}