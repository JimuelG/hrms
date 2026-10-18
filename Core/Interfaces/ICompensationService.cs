using Core.Common;
using Core.DTOs.Payroll;

namespace Core.Interfaces;
public interface ICompensationService
{
    Task<ServiceResult<IReadOnlyList<CompensationDto>>> GetHistoryAsync(Guid employeeId, CancellationToken ct = default);
    Task<ServiceResult<CompensationDto>> SetAsync(Guid actorUserId, Guid employeeId, SetCompensationDto dto, CancellationToken ct = default);
}