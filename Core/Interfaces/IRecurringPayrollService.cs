using Core.Common;
using Core.DTOs.Payroll;

namespace Core.Interfaces;
public interface IRecurringPayrollService
{
    Task<ServiceResult<IReadOnlyList<RecurringItemDto>>> ListAsync(Guid employeeId, CancellationToken ct = default);
    Task<ServiceResult<RecurringItemDto>> CreateAsync(Guid actorUserId, CreateRecurringItemDto dto, CancellationToken ct = default);
    Task<ServiceResult<RecurringItemDto>> DeactivateAsync(Guid id, CancellationToken ct = default);
}