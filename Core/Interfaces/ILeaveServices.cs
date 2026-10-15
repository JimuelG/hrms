using Core.Common;
using Core.DTOs.Leave;
using Core.Entities;
using Core.Enums;

namespace Core.Interfaces;
public interface ILeaveTypeService
{
    Task<ServiceResult<LeaveTypeDto>> CreateAsync(CreateLeaveTypeDto dto, CancellationToken ct = default);
    Task<ServiceResult<LeaveTypeDto>> UpdateAsync(Guid id, UpdateLeaveTypeDto dto, CancellationToken ct = default);
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<LeaveTypeDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<LeaveTypeDto>> GetAllAsync(string? search, bool activeOnly, CancellationToken ct = default);
}

public interface IHolidayService
{
    Task<ServiceResult<IReadOnlyList<HolidayDto>>> GetByYearAsync(int year, CancellationToken ct = default);
    Task<ServiceResult<HolidayDto>> CreateAsync(UpsertHolidayDto dto, CancellationToken ct = default);
    Task<ServiceResult<HolidayDto>> UpdateAsync(Guid id, UpsertHolidayDto dto, CancellationToken ct = default);
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface ILeaveCalculationService
{
    Task<ServiceResult<LeavePreviewDto>> PreviewAsync(Guid userId, LeavePreviewQuery query, CancellationToken ct = default);
    Task<ServiceResult<LeaveCalculation>> CalculateAsync(
        Employee employee, LeaveType type, DateOnly start, DateOnly end,
         DayPortion startPortion, DayPortion endPortion, CancellationToken ct = default);
}