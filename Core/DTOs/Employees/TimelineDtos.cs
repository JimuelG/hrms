using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs.Employees;
public sealed record TimelineEventDto(
    Guid Id,
    TimeLineEventType EventType,
    string Title,
    string? Description,
    DateTime EventDateUtc);

public sealed class AddTimelineNoteDto
{
    [Required, MaxLength(200)]
    public string Title { get; init; } = "";
    [MaxLength(1000)]
    public string? Description { get; init; }
    public DateTime? EventDateUtc { get; init; }
}