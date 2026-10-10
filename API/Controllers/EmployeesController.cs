using API.Authorization;
using API.Extensions;
using Core.Constant;
using Core.DTOs.Attendance;
using Core.DTOs.Employees;
using Core.DTOs.Organization;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
public class EmployeesController(IEmployeeService service) 
    : BaseApiController
{
    [HasPermission(Permissions.Employees.Read)]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, CancellationToken ct) =>
        Ok(await service.GetAllAsync(search, ct));

    [HasPermission(Permissions.Employees.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var employee = await service.GetByIdAsync(id, ct);
        return employee is null ? NotFound() : Ok(employee);
    }

    [HasPermission(Permissions.Employees.Write)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateEmployeeDto dto, CancellationToken ct) => 
        (await service.CreateAsync(dto, ct)).ToActionResult(this, isCreate: true);

    [HasPermission(Permissions.Employees.Write)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateEmployeeDto dto, CancellationToken ct) => 
        (await service.UpdateAsync(id, dto, ct)).ToActionResult(this);

    [HasPermission(Permissions.Employees.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        (await service.DeleteAsync(id, ct)).ToActionResult(this);

    [HasPermission(Permissions.Employees.Read)]
    [HttpGet("{employeeId:guid}/emergency-contacts")]
    public async Task<IActionResult> GetEmergencyContacts(Guid employeeId, [FromServices] IEmergencyContactService contacts, CancellationToken ct) =>
        Ok(await contacts.GetForEmployeeAsync(employeeId, ct));

    [HasPermission(Permissions.Employees.Write)]
    [HttpPost("{employeeId:guid}/emergency-contacts")]
    public async Task<IActionResult> AddEmergencyContact(Guid employeeId, UpsertEmergencyContactDto dto, [FromServices] IEmergencyContactService contacts, CancellationToken ct) =>
        (await contacts.CreateAsync(employeeId, dto, ct)).ToActionResult(this, isCreate: true);

    [HasPermission(Permissions.Employees.Write)]
    [HttpPut("{employeeId:guid}/emergency-contacts/{contactId:guid}")]
    public async Task<IActionResult> UpdateEmergencyContact(Guid employeeId, Guid contactId, UpsertEmergencyContactDto dto, [FromServices] IEmergencyContactService contacts, CancellationToken ct) =>
        (await contacts.UpdateAsync(employeeId, contactId, dto, ct)).ToActionResult(this);

    [HasPermission(Permissions.Employees.Delete)]
    [HttpDelete("{employeeId:guid}/emergency-contacts/{contactId:guid}")]
    public async Task<IActionResult> DeleteEmergencyContact(Guid employeeId, Guid contactId, [FromServices] IEmergencyContactService contacts, CancellationToken ct) =>
        (await contacts.DeleteAsync(employeeId, contactId, ct)).ToActionResult(this);

    [HasPermission(Permissions.Employees.Read)]
    [HttpGet("{employeeId:guid}/documents")]
    public async Task<IActionResult> GetDocuments(
        Guid employeeId, [FromServices] IEmployeeDocumentService docs, CancellationToken ct) =>
        Ok(await docs.GetForEmployeeAsync(employeeId, ct));

    [HasPermission(Permissions.Employees.Write)]
    [HttpPost("{employeeId:guid}/documents")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadDocument(
        Guid employeeId, [FromForm] string documentType, [FromForm] IFormFile file,
        [FromForm] DateOnly? expirationDate, [FromServices] IEmployeeDocumentService docs, CancellationToken ct)
    {
        if (file.Length == 0) return BadRequest(new { message = "No file was uploaded."});

        await using var stream = file.OpenReadStream();
        var result = await docs.UploadAsync(
            employeeId,
            documentType,
            stream,
            file.Name,
            file.ContentType,
            file.Length,
            expirationDate,
            ct);

        return result.ToActionResult(this, isCreate: true);
    }

    [HasPermission(Permissions.Employees.Write)]
    [HttpGet("{employeeId:guid}/documents/{documentId:guid}/download")]
    public async Task<IActionResult> DownloadDocument(
        Guid employeeId, Guid documentId, [FromServices] IEmployeeDocumentService docs, CancellationToken ct)
    {
        var result = await docs.DownloadAsync(employeeId, documentId, ct);
        if (!result.Succeeded) return result.ToActionResult(this);

        var (content, contentType, fileName) = result.Value;
        return File(content, contentType, fileName);
    }

    [HasPermission(Permissions.Employees.Write)]
    [HttpPost("{employeeId:guid}/documents/{documentId:guid}/verify")]
    public async Task<IActionResult> VerifyDocument(
        Guid employeeId, Guid documentId, VerifyDocumentDto dto, [FromServices] IEmployeeDocumentService docs, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        return (await docs.VerifyAsync(employeeId, documentId, dto, userId, ct)).ToActionResult(this);
    }

    [HasPermission(Permissions.Employees.Delete)]
    [HttpDelete("{employeeId:guid}/documents/{documentId:guid}")]
    public async Task<IActionResult> DeleteDocument(
        Guid employeeId, Guid documentId, [FromServices] IEmployeeDocumentService docs, CancellationToken ct) =>
            (await docs.DeleteAsync(employeeId, documentId, ct)).ToActionResult(this);

    [HasPermission(Permissions.Employees.Read)]
    [HttpGet("{employeeId:guid}/timeline")]
    public async Task<IActionResult> GetTimeline(
        Guid employeeId, [FromServices] ITimelineService timeline, CancellationToken ct) =>
        Ok(await timeline.GetForEmployeeAsync(TimelineSubjectType.Employee, employeeId, ct));

    [HasPermission(Permissions.Employees.Write)]
    [HttpPost("{employeeId:guid}/timeline/notes")]
    public async Task<IActionResult> AddTimelineNote(
        Guid employeeId, AddTimelineNoteDto dto, [FromServices] ITimelineService timeline, [FromServices] IUnitOfWork unit, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        Guid.TryParse(userIdClaim, out var userId);

        timeline.Record(
            TimelineSubjectType.Employee,
            employeeId,
            TimeLineEventType.Note,
            dto.Title,
            dto.Description,
            dto.EventDateUtc,
            userId == Guid.Empty ? null : userId
        );

        await unit.Complete();

        return StatusCode(201);
    }

    [HasPermission(Permissions.Employees.Read)]
    [HttpGet("eligible-managers")]
    public async Task<IActionResult> GetEligibleManagers(
        [FromQuery] Guid? excludeEmployeeId, CancellationToken ct) =>
        Ok(await service.GetEligibleManagersAsync(excludeEmployeeId, ct));

    [HasPermission(Permissions.Employees.Read)]
    [HttpGet("{employeeId:guid}/work-locations")]
    public async Task<IActionResult> GetWorkLocations(
        Guid employeeId, [FromServices] IEmployeeWorkLocationService svc, CancellationToken ct) =>
        Ok(await svc.GetForEmployeeAsync(employeeId, ct));

    [HasPermission(Permissions.Employees.Write)]
    [HttpPut("{employeeId:guid}/work-locations")]
    public async Task<IActionResult> SetWorkLocations(
        Guid employeeId, SetEmployeeWorkLocationsDto dto, [FromServices] IEmployeeWorkLocationService svc, CancellationToken ct) =>
        (await svc.SetForEmployeeAsync(employeeId, dto.WorkLocationIds!, ct)).ToActionResult(this);
    
    [HasPermission(Permissions.Employees.LinkUser)]        
    [HttpPut("{employeeId:guid}/user-link")]
    public async Task<IActionResult> LinkUser(
        Guid employeeId, LinkUserDto dto, [FromServices] IEmployeeUserLinkService svc, CancellationToken ct) =>
        (await svc.LinkAsync(employeeId, dto.Email, ct)).ToActionResult(this);

    [HasPermission(Permissions.Employees.LinkUser)]
    [HttpDelete("{employeeId:guid}/user-link")]
    public async Task<IActionResult> UnlinkUser(
        Guid employeeId, [FromServices] IEmployeeUserLinkService svc, CancellationToken ct) =>
        (await svc.UnLinkAsync(employeeId, ct)).ToActionResult(this);
}