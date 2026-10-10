using Core.Common;
using Core.DTOs.Attendance;

namespace Core.Interfaces;
public interface IAttendanceService
{
    Task<ServiceResult<AttendanceRecordDto>> ClockInAsync(Guid userId, ClockRequestDto dto, CancellationToken ct = default);
    Task<ServiceResult<AttendanceRecordDto>> ClockOutAsync(Guid userId, ClockRequestDto dto, CancellationToken ct = default);
    Task<ServiceResult<IReadOnlyList<AttendanceRecordDto>>> GetMineAsync(Guid userId, DateOnly? from, DateOnly? to, CancellationToken ct = default);
    Task<ServiceResult<PagedResult<AttendanceRecordDto>>> QueryAsync(DateOnly from, DateOnly to, Guid? employeeId, bool needsReview, int page, int pageSize, CancellationToken ct = default);
}