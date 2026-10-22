using Core.Common;
using Core.DTOs.Reports;

namespace Core.Interfaces;
public interface IWorkforceReportService
{
    Task<ServiceResult<WorkforceReportDto>> GetAsync(CancellationToken ct = default);
    Task<ServiceResult<CsvFile>> ExportAsync(string breakdown, CancellationToken ct = default);
}