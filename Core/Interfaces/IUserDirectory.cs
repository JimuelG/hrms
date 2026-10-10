namespace Core.Interfaces;
public interface IUserDirectory
{
    Task<Guid?> FindActiveMemberIdAsync(string email, Guid tenantId, CancellationToken ct = default);
}