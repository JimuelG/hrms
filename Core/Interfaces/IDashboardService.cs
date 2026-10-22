using Core.Common;
using Core.DTOs.Dashboard;

namespace Core.Interfaces;
public interface IDashboardService
{
    Task<ServiceResult<DashboardDto>> GetAsync(Guid userId, CancellationToken ct = default);
}