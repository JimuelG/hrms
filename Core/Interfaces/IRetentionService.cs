namespace Core.Interfaces;
public interface IRetentionService
{
    Task<(int AuditRows, int Alerts)> RunAsync(CancellationToken ct = default);
}