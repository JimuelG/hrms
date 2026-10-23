using Core.Common;
using Core.DTOs.Audit;

namespace Core.Interfaces;
public interface IAuditLogQuery
{
    Task<ServiceResult<AuditPageDto>> SearchAsync(AuditQuery q, CancellationToken ct = default);
    Task<AuditFacetsDto> FacetsAsync(CancellationToken ct = default);
}