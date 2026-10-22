using Core.Common;
using Core.DTOs.Reports;

namespace Core.Interfaces;
public interface IAttendanceReportService
{
    Task<ServiceResult<AttendanceReportDto>> GetAsync(AttendanceReportQuery q, CancellationToken ct = default);
    Task<ServiceResult<CsvFile>> ExportAsync(AttendanceReportQuery q, CancellationToken ct = default);
}

public interface ILeaveReportService
{
    Task<ServiceResult<LeaveUsageReportDto>> UsageAsync(int year, Guid? departmentid, Guid? branchId, CancellationToken ct = default);
    Task<ServiceResult<CsvFile>> ExportUsageAsync(int year, Guid? departmentId, Guid? branchId, CancellationToken ct = default);
    Task<ServiceResult<LeaveCalendarDto>> CalendarAsync(DateOnly from, DateOnly to, Guid? departmentId, Guid? branchId, CancellationToken ct = default);
    Task<ServiceResult<CsvFile>> ExportCalendarAsync(DateOnly from, DateOnly to, Guid? departmentId, Guid? branchId, CancellationToken ct = default);
}