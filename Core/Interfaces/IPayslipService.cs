using Core.Common;
using Core.DTOs.Payroll;

namespace Core.Interfaces;
public interface IPayslipService
{
    Task<ServiceResult<IReadOnlyList<PayslipSummaryDto>>> ListMineAsync(Guid userId, CancellationToken ct = default);
    Task<ServiceResult<PayslipDto>> GetMineAsynce(Guid userId, Guid runId, CancellationToken ct = default);
    Task<ServiceResult<PayslipDto>> GetForEmployeeAsync(Guid runId, Guid employeeId, CancellationToken ct = default);
}