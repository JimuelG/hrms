namespace Core.Interfaces;

public sealed record AlertDto(
    Guid Id,
    int Kind,
    string Title,
    string Message,
    Guid? SubjectId,
    DateTime CreatedAtUtc);

public interface IAlertService
{
    Task<int> ScanAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AlertDto>> ListOpenAsync(CancellationToken ct = default);
}

public interface ILeaveGrantSweep
{
    Task<int> EnsureAllAsync(CancellationToken ct = default);
}