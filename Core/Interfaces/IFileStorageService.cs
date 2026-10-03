namespace Core.Interfaces;

public sealed record StoredFile(
    string StorageKey,
    string OriginalFileName,
    string ContentType,
    long SizeBytes
);
public interface IFileStorageService
{
    Task<StoredFile> SaveAsync(string storageKey, Stream content, string originalFileName, string contentType, CancellationToken ct = default);
    Task<(Stream Content, string ContentType)?> GetAsync(string storageKey, CancellationToken ct = default);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}