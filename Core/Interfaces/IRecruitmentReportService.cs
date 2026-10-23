using Core.Common;
using Core.DTOs.Reports;

namespace Core.Interfaces;
public interface IRecruitmentReportService
{
    Task<ServiceResult<RecruitmentReportDto>> GetAsync(RecruitmentReportQuery query, CancellationToken ct = default);
    Task<ServiceResult<CsvFile>> ExportAsync(RecruitmentReportQuery query, CancellationToken ct = default);
}