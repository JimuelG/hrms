namespace Core.Common;
public static class LeaveAttachmentRules
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public const int MaxPerRequest = 5;

    private static readonly Dictionary<string, string> TypeByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png"
    };

    public static (string? ContentType, string? Error) Check(string fileName, long length, ReadOnlySpan<byte> head, int existingCount)
    {
        if (existingCount >= MaxPerRequest)
            return (null, $"A request can have at most {MaxPerRequest} documents.");
        if (length <= 0) return (null, "The file is empty.");
        if (length > MaxBytes) return (null, $"The file is larger than {MaxBytes / (1024 * 1024)} MB");

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!TypeByExtension.TryGetValue(ext, out var contentType))
            return (null, "Only PDF, JPEG and PNG files are accepted.");

        var matches = ext switch
        {
            ".pdf" => StartsWith(head, 0x25, 0x50, 0x44, 0x46, 0x2D),
            ".png" => StartsWith(head, 0x89, 0x50, 0x4e, 0x47, 0x0D, 0x0A, 0x1A, 0x0A),
            ".jpg" or ".jpeg" => StartsWith(head, 0xFF, 0xD8, 0xFF),
            _ => false
        };

        return matches ? (contentType, null) : (null, "The file's contents don't match its type.");
    }

    private static bool StartsWith(ReadOnlySpan<byte> head, params byte[] signature) =>
        head.Length >= signature.Length && head[..signature.Length].SequenceEqual(signature);
}