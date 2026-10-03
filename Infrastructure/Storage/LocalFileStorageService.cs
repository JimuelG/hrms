using Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Storage;

public sealed class LocalFileStorageService(IConfiguration config)
    : IFileStorageService
{
    private readonly string _root = config["FileStorage:LocalPath"]
        ?? throw new InvalidOperationException("Set FileStorage: LocalPath in configuration.");

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = ResolvePath(storageKey);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task<(Stream Content, string ContentType)?> GetAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = ResolvePath(storageKey);
        if (!File.Exists(fullPath)) return Task.FromResult<(Stream, string)?>(null);

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult<(Stream, string)?>((stream, "application/octet-stream"));
    }

    public async Task<StoredFile> SaveAsync(string storageKey, Stream content, string originalFileName, string contentType, CancellationToken ct = default)
    {
        var fullPath = ResolvePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, ct);

        return new StoredFile(storageKey, originalFileName, contentType, fileStream.Length);
    }

    private string ResolvePath(string storageKey)
    {
        var combined = Path.GetFullPath(Path.Combine(_root, storageKey));
        if (!combined.StartsWith(Path.GetFullPath(_root), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid storage key.");
        return combined;
    }
}