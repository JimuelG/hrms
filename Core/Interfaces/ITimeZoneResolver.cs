using Core.Entities;

namespace Core.Interfaces;
public interface ITimeZoneResolver
{
    Task<string?> ResolveAsync(Employee employee, CancellationToken ct = default);
}