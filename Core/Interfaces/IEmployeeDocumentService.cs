using Core.Common;
using Core.DTOs.Employees;

namespace Core.Interfaces;
public interface IEmployeeDocumentService
{
    Task<IReadOnlyList<EmployeeDocumentDto>> GetForEmployeeAsync(Guid employeeid, CancellationToken ct = default);
    Task<ServiceResult<EmployeeDocumentDto>> UploadAsync(
        Guid employeeId, string documentType, Stream content, string fileName, string contentType, 
        long sizeBytes, DateOnly? expirationDate, CancellationToken ct = default);
    Task<ServiceResult<(Stream Content, string ContentType, string FileName)>> DownloadAsync(
        Guid employeeId, Guid documentId, CancellationToken ct = default);
    Task<ServiceResult<EmployeeDocumentDto>> VerifyAsync(Guid employeeId, Guid documentId, VerifyDocumentDto dto, Guid verifiedByUserId, CancellationToken ct = default);
    Task<ServiceResult<bool>> DeleteAsync(Guid employeeId, Guid documentId, CancellationToken ct = default);
}