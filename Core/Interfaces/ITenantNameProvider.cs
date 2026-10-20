namespace Core.Interfaces;
public interface ITenantNameProvider
{
    Task<string?> GetCurrentNameAsync(CancellationToken ct = default);
}