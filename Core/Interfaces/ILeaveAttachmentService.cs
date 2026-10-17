using Core.Common;
using Core.DTOs.Leave;

namespace Core.Interfaces;
public interface ILeaveAttachmentService
{
    Task<ServiceResult<IReadOnlyList<LeaveAttachmentDto>>> ListAsync(Guid userId, Guid requestId, CancellationToken ct = default);
    Task<ServiceResult<LeaveAttachmentDto>> UploadAsync(Guid userId, Guid requestId, string fileName, Stream content, long length, CancellationToken ct =default);    
    Task<ServiceResult<LeaveAttachmentFile>> DownloadAsync(Guid userId, Guid requestId, Guid attachmentId, CancellationToken ct = default);
    Task<ServiceResult<bool>> DeleteAsync(Guid userId, Guid requestId, Guid attachmentId, CancellationToken ct = default);
}