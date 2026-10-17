namespace Core.Interfaces;
public interface IPermissionDirectory
{
    Task<IReadOnlyDictionary<string, IReadOnlySet<Guid>>> GetHolderUserIdsAsync(IReadOnlyCollection<string> permissions, CancellationToken ct = default);
}