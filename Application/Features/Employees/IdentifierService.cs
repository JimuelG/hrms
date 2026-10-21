using Core.Common;
using Core.DTOs.Employees;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Employees;

public class IdentifierService(
    IUnitOfWork unit,
    IGenericRepository<EmployeeIdentifiers> rows,
    IGenericRepository<IdentifierAccessLog> log,
    IGenericRepository<Employee> employees,
    ISecretProtector protector,
    IUserDirectory users,
    ITenantContext tenant) : IIdentifierService
{
    private const string NoEmployee = "Employee not found.";
    public async Task<ServiceResult<IReadOnlyList<IdentifierAccessDto>>> AccessLogAsync(Guid employeeId, CancellationToken ct = default)
    {
        if (await employees.GetByIdAsync(employeeId, ct) is null)
            return ServiceResult<IReadOnlyList<IdentifierAccessDto>>.Fail(NoEmployee, ServiceErrorType.NotFound);

        var entries = (await log.ListAsync(new IdentifierAccessByEmployeeSpecification(employeeId), ct)).Take(50).ToList();
        var names = await users.GetDisplayNamesAsync(entries.Select(e => e.UserId).Distinct().ToList(), tenant.RequiredTenantId, ct);

        return ServiceResult<IReadOnlyList<IdentifierAccessDto>>.Success(entries
            .Select(e => new IdentifierAccessDto((int)e.Field, (int)e.Action, names.GetValueOrDefault(e.UserId, "Unknown"), e.CreatedAtUtc))
            .ToList());
    }

    public async Task<ServiceResult<IdentifiersDto>> GetAsync(Guid employeeId, CancellationToken ct = default)
    {
        if (await employees.GetByIdAsync(employeeId, ct) is null)
            return ServiceResult<IdentifiersDto>.Fail(NoEmployee, ServiceErrorType.NotFound);

        var row = (await rows.ListAsync(new IdentifiersByEmployeeSpecification(employeeId), ct)).FirstOrDefault();
        return ServiceResult<IdentifiersDto>.Success(ToDto(employeeId, row));
    }

    public async Task<ServiceResult<RevealedDto>> RevealAsync(Guid actorUserId, Guid employeeId, int field, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(typeof(IdentifierField), field))
            return ServiceResult<RevealedDto>.Fail("Choose a valid field.");
        if (await employees.GetByIdAsync(employeeId, ct) is null)
            return ServiceResult<RevealedDto>.Fail(NoEmployee, ServiceErrorType.NotFound);

        var f = (IdentifierField)field;
        var row = (await rows.ListAsync(new IdentifiersByEmployeeSpecification(employeeId), ct)).FirstOrDefault();
        var stored = row is null ? null : Stored(row, f);
        if (stored is null)
            return ServiceResult<RevealedDto>.Fail($"No {IdentifierRules.Label(f)} is recorded.", ServiceErrorType.NotFound);

        if (!protector.TryUnprotect(tenant.RequiredTenantId, stored, out var value))
            return ServiceResult<RevealedDto>.Fail("This value can't be decrypted. The encryption keys may have changed.", ServiceErrorType.Conflict);

        log.Add(new IdentifierAccessLog { EmployeeId = employeeId, UserId = actorUserId, Field = f, Action = IdentifierAction.Revealed });
        await unit.Complete();

        return ServiceResult<RevealedDto>.Success(new RevealedDto(field, value));
    }

    public async Task<ServiceResult<IdentifiersDto>> SetAsync(Guid actorUserId, Guid employeeId, UpdateIdentifiersDto dto, CancellationToken ct = default)
    {
        if (await employees.GetByIdAsync(employeeId, ct) is null)
            return ServiceResult<IdentifiersDto>.Fail(NoEmployee, ServiceErrorType.NotFound);

        var scope = tenant.RequiredTenantId;
        var plan = new List<(IdentifierField Field, string? Protected, IdentifierAction Action)>();

        foreach (var (field, input) in new (IdentifierField, string?)[]
        {
            (IdentifierField.Tin, dto.Tin),
            (IdentifierField.Sss, dto.Sss),
            (IdentifierField.PhilHealth, dto.PhilHealth),
            (IdentifierField.PagIbig, dto.PagIbig),
            (IdentifierField.BankAccountNumber, dto.BankAccountNumber)
        })
        {
            if (input is null) continue;
            if (string.IsNullOrWhiteSpace(input)) { plan.Add((field, null, IdentifierAction.Cleared)); continue; }

            var (value, error) = IdentifierRules.Check(field, input);
            if (error is not null) return ServiceResult<IdentifiersDto>.Fail(error);
            plan.Add((field, protector.Protect(scope, value!), IdentifierAction.Updated));
        }

        var bankName = dto.BankName?.Trim();
        var accountName = dto.BankAccountName?.Trim();
        if (bankName is { Length: > 100} || accountName is { Length: > 150 })
            return ServiceResult<IdentifiersDto>.Fail("The bank name or account name is too long.");

        var row = (await rows.ListAsync(new IdentifiersByEmployeeSpecification(employeeId), ct)).FirstOrDefault();
        var isNew = row is null;
        row ??= new EmployeeIdentifiers { EmployeeId = employeeId };

        foreach (var (field, value, action) in plan)
        {
            switch (field)
            {
                case IdentifierField.Tin: row.TinProtected = value;
                    break;
                case IdentifierField.Sss: row.SssProtected = value;
                    break;
                case IdentifierField.PhilHealth: row.PhilHealthProtected = value;
                    break;
                case IdentifierField.PagIbig: row.PagIbigProtected = value;
                    break;
                default: row.BankAccountNumberProtected = value;
                    break;
            }
            log.Add(new IdentifierAccessLog { EmployeeId = employeeId, UserId = actorUserId, Field = field, Action = action });
        }

        if (bankName is not null) row.BankName = bankName.Length == 0 ? null : bankName;
        if (accountName is not null) row.BankAccountName = accountName.Length == 0 ? null : accountName;
        row.UpdatedByUserId = actorUserId;

        if (isNew) rows.Add(row);
        await unit.Complete();

        return ServiceResult<IdentifiersDto>.Success(ToDto(employeeId, row));
    }

    private static string? Stored(EmployeeIdentifiers r, IdentifierField f) => f switch
    {
        IdentifierField.Tin => r.TinProtected,
        IdentifierField.Sss => r.SssProtected,
        IdentifierField.PhilHealth => r.PhilHealthProtected,
        IdentifierField.PagIbig => r.PagIbigProtected,
        _ => r.BankAccountNumberProtected
    };

    private IdentifiersDto ToDto(Guid employeeId, EmployeeIdentifiers? r)
    {
        if (r is null) return new(employeeId, null, null, null, null, null, null, null, null);

        string? Masked(string? stored)
        {
            if (stored is null) return null;
            return protector.TryUnprotect(tenant.RequiredTenantId, stored, out var v) ? IdentifierRules.Mask(v) : "(unreadable)";
        }

        return new(
            employeeId,
            Masked(r.TinProtected),
            Masked(r.SssProtected),
            Masked(r.PhilHealthProtected),
            Masked(r.PagIbigProtected),
            r.BankName,
            r.BankAccountName,
            Masked(r.BankAccountNumberProtected),
            r.UpdatedAtUtc ?? r.CreatedAtUtc);
    }
}