namespace Core.Interfaces;
public interface IUserDirectory
{
    Task<Guid?> FindActiveMemberIdAsync(string email, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IReadOnlyCollection<Guid> userIds, Guid tenantId, CancellationToken ct = default);
}