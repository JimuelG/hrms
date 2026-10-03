using Core.Common;
using Core.DTOs.Organization;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Employees;

public sealed class EmergencyContactService(
    IUnitOfWork unit,
    IGenericRepository<EmployeeEmergencyContact> contacts,
    IGenericRepository<Employee> employees)
    : IEmergencyContactService
{
    public async Task<ServiceResult<EmergencyContactDto>> CreateAsync(Guid employeeId, UpsertEmergencyContactDto dto, CancellationToken ct = default)
    {
        if (await employees.GetByIdAsync(employeeId, ct) is null)
            return ServiceResult<EmergencyContactDto>.Fail("Employee not found.", ServiceErrorType.NotFound);
        
        var contact = new EmployeeEmergencyContact
        {
            EmployeeId = employeeId,
            Name = dto.Name,
            Relationship = dto.Relationship,
            Phone = dto.Phone,
            AlternatePhone = dto.AlternatePhone,
            Address = dto.Address,
            IsPrimary = dto.IsPrimary
        };
        contacts.Add(contact);
        await unit.Complete();
        return ServiceResult<EmergencyContactDto>.Success(ToDto(contact));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid employeeId, Guid contactId, CancellationToken ct = default)
    {
        var contact = await contacts.GetByIdAsync(contactId, ct);
        if (contact is null || contact.EmployeeId != employeeId)
            return ServiceResult<bool>.Fail("Emergency contact not found.", ServiceErrorType.NotFound);

        contacts.Remove(contact);
        await unit.Complete();
        return ServiceResult<bool>.Success(true);
    }

    public async Task<IReadOnlyList<EmergencyContactDto>> GetForEmployeeAsync(Guid employeeId, CancellationToken ct = default) =>
        (await contacts.ListAsync(new EmergencyContactsByEmployeeSpecification(employeeId), ct)).Select(ToDto).ToList();

    public async Task<ServiceResult<EmergencyContactDto>> UpdateAsync(Guid employeeId, Guid contractId, UpsertEmergencyContactDto dto, CancellationToken ct = default)
    {
        var contact = await contacts.GetByIdAsync(contractId, ct);
        if (contact is null || contact.EmployeeId != employeeId)
            return ServiceResult<EmergencyContactDto>.Fail("Emergency contact not found", ServiceErrorType.NotFound);

        contact.Name = dto.Name;
        contact.Relationship = dto.Relationship;
        contact.Phone = dto.Phone;
        contact.AlternatePhone = dto.AlternatePhone;
        contact.Address = dto.Address;
        contact.IsPrimary = dto.IsPrimary;

        await unit.Complete();
        return ServiceResult<EmergencyContactDto>.Success(ToDto(contact));
    }

    private static EmergencyContactDto ToDto(EmployeeEmergencyContact c) =>
        new(
            c.Id,
            c.Name,
            c.Relationship,
            c.Phone,
            c.AlternatePhone,
            c.Address,
            c.IsPrimary
        );
}