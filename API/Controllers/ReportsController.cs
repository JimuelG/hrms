using System.Text;
using API.Authorization;
using API.Extensions;
using Core.Common;
using Core.Constant;
using Core.DTOs.Reports;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class ReportsController(
    IWorkforceReportService workforce,
    IAttendanceReportService attendance,
    ILeaveReportService leave) : BaseApiController
{
    private IActionResult Csv(ServiceResult<CsvFile> result)
    {
        if (!result.Succeeded) return result.ToActionResult(this);
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(result.Value!.Content)).ToArray();
        return File(bytes, "text/csv; charset=utf-8", result.Value.FileName);    
    }

    private void NoStore() => Response.Headers.CacheControl = "private, no-store";

    [HasPermission(Permissions.Reports.View)]
    [HttpGet("workforce")]
    public async Task<IActionResult> Workforce(CancellationToken ct)
    {
        NoStore();
        return (await workforce.GetAsync(ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Reports.View)]
    [HttpGet("workforce/export")]
    public async Task<IActionResult> ExportWorkforce([FromQuery] string breakdown, CancellationToken ct) =>
        Csv(await workforce.ExportAsync(breakdown, ct));
    
    [HasPermission(Permissions.Reports.View)]
    [HttpGet("attendance")]
    public async Task<IActionResult> Attendance([FromQuery] AttendanceReportQuery query, CancellationToken ct)
    {
        NoStore();
        return (await attendance.GetAsync(query, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Reports.View)]
    [HttpGet("attendance/export")]
    public async Task<IActionResult> ExportAttendance([FromQuery] AttendanceReportQuery query, CancellationToken ct) =>
        Csv(await attendance.ExportAsync(query, ct));

    [HasPermission(Permissions.Reports.View)]
    [HttpGet("leave-usage")]
    public async Task<IActionResult> LeaveUsage([FromQuery] int year, [FromQuery] Guid? departmentId, [FromQuery] Guid? branchId, CancellationToken ct)
    {
        NoStore();
        return (await leave.UsageAsync(year, departmentId, branchId, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Reports.View)]
    [HttpGet("leave-usage/export")]
    public async Task<IActionResult> ExportLeaveUsage([FromQuery] int year, [FromQuery] Guid? departmentId, [FromQuery] Guid? branchId, CancellationToken ct) =>
        Csv(await leave.ExportUsageAsync(year, departmentId, branchId, ct));
    
    [HasPermission(Permissions.Reports.View)]
    [HttpGet("leave-calendar")]
    public async Task<IActionResult> LeaveCalendar([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] Guid? departmentId, [FromQuery] Guid? branchId, CancellationToken ct)
    {
        NoStore();
        return (await leave.CalendarAsync(from, to, departmentId, branchId, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Reports.View)]
    [HttpGet("leave-calendar/export")]
    public async Task<IActionResult> ExportLeaveCalendar([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] Guid? departmentId, [FromQuery] Guid? branchId, CancellationToken ct) =>
        Csv(await leave.ExportCalendarAsync(from, to, departmentId, branchId, ct));

}