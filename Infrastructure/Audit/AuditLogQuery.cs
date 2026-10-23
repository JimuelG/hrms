using System.Security.AccessControl;
using Core.Common;
using Core.DTOs.Audit;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Audit;

public sealed class AuditLogQuery(
    AppDbContext db,
    ITenantContext tenant,
    IUserDirectory users) : IAuditLogQuery
{
    private const int MaxUserOptions = 200;

    private IQueryable<AuditLog> Scoped()
    {
        var tenantId = tenant.RequiredTenantId;
        var hidden = AuditRules.HiddenEntities;
        return db.AuditLogs.AsNoTracking().Where(a => a.TenantId == tenantId && !hidden.Contains(a.EntityName));
    }
    public async Task<AuditFacetsDto> FacetsAsync(CancellationToken ct = default)
    {
        var scoped = Scoped();
        var entities = await scoped.Select(a => a.EntityName).Distinct().OrderBy(e => e).ToListAsync(ct);
        var actions = await scoped.Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync(ct);
        var userIds = await scoped.Where(a => a.UserId != null).Select(a => a.UserId!.Value).Distinct().Take(MaxUserOptions).ToListAsync(ct);

        var names = await users.GetDisplayNamesAsync(userIds, tenant.RequiredTenantId, ct);
        var options = userIds.Select(id => new AuditUserOption(id, names.GetValueOrDefault(id, "Unknown user")))
            .OrderBy(o => o.Name).ToList();

        return new AuditFacetsDto(entities, actions, options);
    }

    public async Task<ServiceResult<AuditPageDto>> SearchAsync(AuditQuery q, CancellationToken ct = default)
    {
        var error = AuditRules.CheckRange(q.From, q.To);
        if (error is not null) return ServiceResult<AuditPageDto>.Fail(error);
        var (page, size) = AuditRules.ClampPage(q.Page, q.PageSize);

        var query = Scoped();
        if (!string.IsNullOrWhiteSpace(q.Entity)) query = query.Where(a => a.EntityName == q.Entity);
        if (!string.IsNullOrWhiteSpace(q.EntityId)) query = query.Where(a => a.EntityId == q.EntityId);
        if (!string.IsNullOrWhiteSpace(q.Action)) query = query.Where(a => a.Action == q.Action);
        if (q.UserId is Guid uid) query = query.Where(a => a.UserId == uid);
        if (q.From is DateOnly from)
        {
            var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(a => a.TimestampUtc >= fromUtc);
        }
        if (q.To is DateOnly to)
        {
            var toUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(a => a.TimestampUtc < toUtc);
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(a => a.TimestampUtc).ThenByDescending(a => a.Id)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);

        var names = await users.GetDisplayNamesAsync(
            rows.Where(r => r.UserId != null).Select(r => r.UserId!.Value).Distinct().ToList(), tenant.RequiredTenantId, ct);

        var typeCache = new Dictionary<string, Func<string, Type?>>();

        var items = rows.Select(r => new AuditEntryDto(
            r.Id,
            r.TimestampUtc,
            r.Action,
            r.EntityName,
            r.EntityId,
            r.UserId,
            r.UserId is Guid u ? names.GetValueOrDefault(u, "Unknown user") : "System",
            r.IpAddress,
            AuditRules.Changes(r.EntityName, r.OldValues, r.NewValues, FieldTypes(r.EntityName, typeCache)))).ToList();

        return ServiceResult<AuditPageDto>.Success(new AuditPageDto(items, total, page, size));
    }

    private Func<string, Type?> FieldTypes(string entityName, Dictionary<string, Func<string, Type?>> cache)
    {
        if (cache.TryGetValue(entityName, out var known)) return known;

        var entityType = db.Model.GetEntityTypes().FirstOrDefault(t => t.ClrType.Name == entityName);
        Func<string, Type?> lookup = field => entityType?.FindProperty(field)?.ClrType;
        cache[entityName] = lookup;
        return lookup;
    }
}