using System.Security.AccessControl;
using Core.Common;

namespace Core.DTOs.Audit;

public sealed class AuditQuery
{
    public string? Entity { get; set; }
    public string? EntityId { get; set; }
    public string? Action { get; set; }
    public Guid? UserId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = AuditRules.DefaultPageSize;
}

public sealed record AuditEntryDto(
    Guid Id,
    DateTime TimestamptUtc,
    string Action,
    string EntityName,
    string EntityId,
    Guid? UserId,
    string UserName,
    string? IpAddress,
    IReadOnlyList<FieldChange> Changes);

public sealed record AuditPageDto(
    IReadOnlyList<AuditEntryDto> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record AuditUserOption(Guid Id, string Name);

public sealed record AuditFacetsDto(
    IReadOnlyList<string> Entities, 
    IReadOnlyList<string> Actions,
    IReadOnlyList<AuditUserOption> Users);