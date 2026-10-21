using Core.Common;
using Core.DTOs.Employees;

namespace Core.Interfaces;
public interface IIdentifierService
{
    Task<ServiceResult<IdentifiersDto>> GetAsync(Guid employeeId, CancellationToken ct = default);
    Task<ServiceResult<IdentifiersDto>> SetAsync(Guid actorUserId, Guid employeeId, UpdateIdentifiersDto dto, CancellationToken ct = default);
    Task<ServiceResult<RevealedDto>> RevealAsync(Guid actorUserId, Guid employeeId, int field, CancellationToken ct = default);
    Task<ServiceResult<IReadOnlyList<IdentifierAccessDto>>> AccessLogAsync(Guid employeeId, CancellationToken ct = default);
}