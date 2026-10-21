using Core.Common;
using Core.DTOs.Payroll;

namespace Core.Interfaces;
public interface IPayrollRunService
{
    Task<IReadOnlyList<PayrollRunSummaryDto>> ListAsync(CancellationToken ct = default);
    Task<ServiceResult<PayrollRunDetailDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<ServiceResult<PayrollRunDetailDto>> StartAsync(Guid actorUserId, StartRunDto dto, CancellationToken ct = default);
    Task<ServiceResult<PayrollRunDetailDto>> RecomputeAsync(Guid actorUserId, Guid id, CancellationToken ct = default);
    Task<ServiceResult<IReadOnlyList<PayrollAdjustmentDto>>> GetAdjustmentsAsync(Guid runId, CancellationToken ct = default);
    Task<ServiceResult<PayrollRunDetailDto>> AddAdjustmentAsync(Guid actorUserId, Guid runId, AddAdjustmentDto dto, CancellationToken ct = default);
    Task<ServiceResult<PayrollRunDetailDto>> RemoveAdjustmentAsync(Guid actorUserId, Guid runId, Guid adjustmentId, CancellationToken ct = default);
    Task<ServiceResult<PayrollRunDetailDto>> ApproveAsync(Guid actorUserId, Guid runId, CancellationToken ct = default);
    Task<ServiceResult<PayrollRunDetailDto>> SendBackAsync(Guid actorUserId, Guid runId, string? reason, CancellationToken ct = default);
    Task<ServiceResult<PayrollRunDetailDto>> FinalizeAsync(Guid actorUserId, Guid runId, CancellationToken ct = default);
    Task<ServiceResult<PayrollRunDetailDto>> CancelAsync(Guid actorUserId, Guid runId, string? reason, CancellationToken ct = default);
    Task<PayrollSettingsDto> GetSettingsAsync(CancellationToken ct = default);
    Task<ServiceResult<PayrollSettingsDto>> UpdateSettingsAsync(UpdatePayrollSettingsDto dto, CancellationToken ct = default);
    Task<ServiceResult<PayrollRunDetailDto>> StartThirteenthAsync(Guid actorUserId, StartThirteenthDto dto, CancellationToken ct = default);
}