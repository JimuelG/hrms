namespace Core.Interfaces;
public interface IPermissionService
{
    Task<IReadOnlySet<string>> GetPermissionsAsync(Guid userId, Guid tenantId, CancellationToken ct = default);
    void InvalidateCache(Guid userId, Guid tenantId);
}