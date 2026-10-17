using Core.Common;
using Core.DTOs.Leave;
using Core.Enums;

namespace Core.Interfaces;
public interface ILeaveRequestService
{
    Task<ServiceResult<LeaveFilingRequestDto>> FileAsync(Guid userId, CreateLeaveRequestDto dto, CancellationToken ct = default);
    Task<ServiceResult<LeaveRequestDto>> GetAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<ServiceResult<PagedResult<LeaveRequestDto>>> GetMineAsync(Guid userId, ApprovalStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task<ServiceResult<PagedResult<LeaveRequestDto>>> QueryAsync(ApprovalStatus? status, Guid? employeeId, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default);
    Task<ServiceResult<LeaveRequestDto>> CancelAsync(Guid userId, Guid id, string? reason, CancellationToken ct = default);
}