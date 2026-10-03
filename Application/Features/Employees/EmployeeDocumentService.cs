using Core.Common;
using Core.DTOs.Employees;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Employees;

public sealed class EmployeeDocumentService(
    IUnitOfWork unit,
    IGenericRepository<EmployeeDocument> documents,
    IGenericRepository<Employee> employees,
    IFileStorageService storage,
    ITenantContext tenant,
    ITimelineService timeline) : IEmployeeDocumentService
{
    private static readonly HashSet<string> AllowedContentType = [
        "application/pdf",
        "image/jpeg",
        "image/png",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"];

    private const long MaxSizeBytes = 10 * 2024 * 2024;

    public async Task<ServiceResult<bool>> DeleteAsync(Guid employeeId, Guid documentId, CancellationToken ct = default)
    {
        var document = await documents.GetByIdAsync(documentId, ct);
        if (document is null || document.EmployeeId != employeeId)
            return ServiceResult<bool>.Fail("Document not found.", ServiceErrorType.NotFound);

        documents.Remove(document);
        await unit.Complete();
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<(Stream Content, string ContentType, string FileName)>> DownloadAsync(Guid employeeId, Guid documentId, CancellationToken ct = default)
    {
        var document = await documents.GetByIdAsync(documentId, ct);
        if (document is null || document.EmployeeId != employeeId)
            return ServiceResult<(Stream, string, string)>.Fail("Document not found.", ServiceErrorType.NotFound);

        var stored = await storage.GetAsync(document.StorageKey, ct);
        if (stored is null)
            return ServiceResult<(Stream, string, string)>.Fail("Stored file is missing.", ServiceErrorType.NotFound);

        return ServiceResult<(Stream, string, string)>.Success((stored.Value.Content, document.ContentType, document.OriginalFileName));
    }

    public async Task<IReadOnlyList<EmployeeDocumentDto>> GetForEmployeeAsync(Guid employeeid, CancellationToken ct = default) =>
        (await documents.ListAsync(new EmployeeDocumentsByEmployeeSpecification(employeeid), ct)).Select(ToDto).ToList();

    public async Task<ServiceResult<EmployeeDocumentDto>> UploadAsync(Guid employeeId, string documentType, Stream content, string fileName, string contentType, long sizeBytes, DateOnly? expirationDate, CancellationToken ct = default)
    {
        if (await employees.GetByIdAsync(employeeId, ct) is null)
            return ServiceResult<EmployeeDocumentDto>.Fail("Employee not found.", ServiceErrorType.NotFound);

        if (!AllowedContentType.Contains(contentType))
            return ServiceResult<EmployeeDocumentDto>.Fail("Unsupported file type. Allowed PDF, JPEG, PNG, DOC, DOCX.");
        
        if (sizeBytes <= 0 || sizeBytes > MaxSizeBytes)
            return ServiceResult<EmployeeDocumentDto>.Fail("File must be between 1 byte and 10 MB. ");

        var safeFileName = Path.GetFileName(fileName);
        var storageKey = $"tenant/{tenant.RequiredTenantId}/employees/{employeeId}/documents/{Guid.NewGuid()}-{safeFileName}";
        
        var saved = await storage.SaveAsync(storageKey, content, safeFileName, contentType, ct);

        var document = new EmployeeDocument
        {
            EmployeeId = employeeId,
            DocumentType = documentType,
            OriginalFileName = saved.OriginalFileName,
            ContentType = saved.ContentType,
            SizeBytes = saved.SizeBytes,
            StorageKey = saved.StorageKey,
            ExpirationDate = expirationDate
        };

        documents.Add(document);
        timeline.Record(employeeId, TimeLineEventType.DocumentUploaded, $"Uploaded: {documentType}", saved.OriginalFileName);
        await unit.Complete();

        return ServiceResult<EmployeeDocumentDto>.Success(ToDto(document));
    }

    public async Task<ServiceResult<EmployeeDocumentDto>> VerifyAsync(Guid employeeId, Guid documentId, VerifyDocumentDto dto, Guid verifiedByUserId, CancellationToken ct = default)
    {
        if ((DocumentStatus)dto.Status == DocumentStatus.Pending)
            return ServiceResult<EmployeeDocumentDto>.Fail("Status must be Verified or Rejected.");

        if ((DocumentStatus)dto.Status == DocumentStatus.Rejected && string.IsNullOrWhiteSpace(dto.RejectionReason))
            return ServiceResult<EmployeeDocumentDto>.Fail("A rejection reason is required when rejecting a document.");

        var document = await documents.GetByIdAsync(documentId, ct);
        if (document is null || document.EmployeeId != employeeId)
            return ServiceResult<EmployeeDocumentDto>.Fail("Document not found.", ServiceErrorType.NotFound);

        document.Status = (DocumentStatus)dto.Status;
        document.VerifiedByUserId = verifiedByUserId;
        document.VerifiedAtUtc = DateTime.UtcNow;
        document.RejectionReason = (DocumentStatus)dto.Status == DocumentStatus.Rejected ? dto.RejectionReason : null;

        timeline.Record(employeeId,
            (DocumentStatus)dto.Status == DocumentStatus.Verified ? TimeLineEventType.DocumentVerified : TimeLineEventType.DocumentRejected,
            $"{document.DocumentType} {((DocumentStatus)dto.Status == DocumentStatus.Verified ? "verified" : "rejected")}",
            (DocumentStatus)dto.Status == DocumentStatus.Rejected ? dto.RejectionReason : null,
            recorededByUserId: verifiedByUserId);
        await unit.Complete();
        return ServiceResult<EmployeeDocumentDto>.Success(ToDto(document));
    }

    private static EmployeeDocumentDto ToDto(EmployeeDocument d) =>
        new(
            d.Id,
            d.DocumentType,
            d.OriginalFileName,
            d.ContentType,
            d.SizeBytes,
            (int)d.Status,
            d.VerifiedAtUtc,
            d.RejectionReason,
            d.ExpirationDate,
            d.UploadedAtUtc);
}