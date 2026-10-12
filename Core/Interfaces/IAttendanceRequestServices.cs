using Core.Common;
using Core.DTOs.Attendance;
using Core.Enums;

namespace Core.Interfaces;
public interface IAttendanceCorrectionService
{
    Task<ServiceResult<AttendanceCorrectionDto>> CreateForSelfAsync(Guid userId, CreateCorrectionDto dto, CancellationToken ct = default);
    Task<ServiceResult<AttendanceCorrectionDto>> CreateForEmployeeAsync(Guid reviewerUserId, Guid employeeId, CreateCorrectionDto dto, CancellationToken ct = default);
    Task<ServiceResult<AttendanceCorrectionDto>> CancelAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<ServiceResult<AttendanceCorrectionDto>> ApproveAsync(Guid reviewerUserId, Guid id, string? note, CancellationToken ct = default);
    Task<ServiceResult<AttendanceCorrectionDto>> RejectAsync(Guid reviewerUserId, Guid id, string? note, CancellationToken ct = default);
    Task<ServiceResult<IReadOnlyList<AttendanceCorrectionDto>>> GetMineAsync(Guid userId, CancellationToken ct = default);
    Task<PagedResult<AttendanceCorrectionDto>> QueryAsync(ApprovalStatus? status, Guid? employeeId, int page, int pageSize, CancellationToken ct = default);
}

public interface IOvertimeService
{
    Task<ServiceResult<OvertimeRequestDto>> CreateAsync(Guid userId, CreateOvertimeDto dto, CancellationToken ct = default);
    Task<ServiceResult<OvertimeRequestDto>> CancelAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<ServiceResult<OvertimeRequestDto>> ApproveAsync(Guid reviewerUserId, Guid id, ApproveOvertimeDto dto, CancellationToken ct = default);
    Task<ServiceResult<OvertimeRequestDto>> RejectAsync(Guid reviewerUserId, Guid id, string? note, CancellationToken ct = default);
    Task<ServiceResult<IReadOnlyList<OvertimeRequestDto>>> GetMineAsync(Guid userId, CancellationToken ct = default);
    Task<PagedResult<OvertimeRequestDto>> QueryAsync(ApprovalStatus? status, Guid? employeeId, int page, int pageSize, CancellationToken ct = default);
}