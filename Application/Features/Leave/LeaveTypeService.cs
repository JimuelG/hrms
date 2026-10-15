using Core.Common;
using Core.DTOs.Leave;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Specifications;

namespace Application.Features.Leave;

public class LeaveTypeService(IUnitOfWork unit, IGenericRepository<LeaveType> repo) : ILeaveTypeService
{
    public async Task<ServiceResult<LeaveTypeDto>> CreateAsync(CreateLeaveTypeDto dto, CancellationToken ct = default)
    {
        var error = Validate(dto);
        if (error is not null) return ServiceResult<LeaveTypeDto>.Fail(error);

        var code = dto.Code.Trim().ToUpperInvariant();
        if (await repo.CountAsync(new LeaveTypeByCodeSpecification(code), ct) > 0)
            return ServiceResult<LeaveTypeDto>.Fail($"A leave type with code '{code}' already exists.", ServiceErrorType.Conflict);

        var type = new LeaveType();
        Apply(type, dto, code);
        repo.Add(type);
        await unit.Complete();
        return ServiceResult<LeaveTypeDto>.Success(ToDto(type));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var type = await repo.GetByIdAsync(id, ct);
        if (type is null) return ServiceResult<bool>.Fail("Leave type not found.", ServiceErrorType.NotFound);

        repo.Remove(type);
        await unit.Complete();
        return ServiceResult<bool>.Success(true);
    }

    public async Task<IReadOnlyList<LeaveTypeDto>> GetAllAsync(string? search, bool activeOnly, CancellationToken ct = default) =>
        (await repo.ListAsync(new LeaveTypeSearchSpecification(search, activeOnly), ct)).Select(ToDto).ToList();

    public async Task<LeaveTypeDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var type = await repo.GetByIdAsync(id, ct);
        return type is null ? null : ToDto(type);
    }

    public async Task<ServiceResult<LeaveTypeDto>> UpdateAsync(Guid id, UpdateLeaveTypeDto dto, CancellationToken ct = default)
    {
        var type = await repo.GetByIdAsync(id, ct);
        if (type is null) return ServiceResult<LeaveTypeDto>.Fail("Leave type not found.", ServiceErrorType.NotFound);

        var error = Validate(dto);
        if (error is not null) return ServiceResult<LeaveTypeDto>.Fail(error);

        var code = dto.Code.Trim().ToUpperInvariant();
        if (await repo.CountAsync(new LeaveTypeByCodeSpecification(code, excludeId: id), ct) > 0)
            return ServiceResult<LeaveTypeDto>.Fail($"A leave type with code '{code}' already exists.", ServiceErrorType.Conflict);

        Apply(type, dto, code);
        type.IsActive = dto.IsActive;
        await unit.Complete();
        return ServiceResult<LeaveTypeDto>.Success(ToDto(type));
    }

    private static string? Validate(CreateLeaveTypeDto dto)
    {
        if (!Enum.IsDefined(typeof(LeaveEntitlementMode), dto.EntitlementMode))
            return "Choose a valid entitlement mode.";
        
        if (!IsHalfDayStep(dto.DefaultDays) || !IsHalfDayStep(dto.MaxCarryOversDays))
            return "Days must be between 0 and 366, in steps of half a day.";

        if ((LeaveEntitlementMode)dto.EntitlementMode == LeaveEntitlementMode.Annual && dto.DefaultDays <= 0)
            return "An annual leave type needs a yearly entitlement greater than zero.";

        return null;
    }

    public static bool IsHalfDayStep(decimal v) => v >= 0 && v <= 366 && v % 0.5m == 0;

    private static void Apply(LeaveType t, CreateLeaveTypeDto dto, string code)
    {
        var mode = (LeaveEntitlementMode)dto.EntitlementMode;

        t.Code = code;
        t.Name = dto.Name.Trim();
        t.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        t.IsPaid = dto.IsPaid;
        t.EntitlementMode = mode;
        t.DefaultDays = mode == LeaveEntitlementMode.Unlimited ? 0 : dto.DefaultDays;
        t.ProrateFirstYear = mode == LeaveEntitlementMode.Annual && dto.ProrateFirstYear;
        t.MaxCarryOverDays = mode == LeaveEntitlementMode.Annual ? dto.MaxCarryOversDays : 0;
        t.MinServiceMonths = dto.MinServiceMonths;
        t.AllowsHalfDay = dto.AllowsHalfDay;
        t.RequiresDocumentAfterDays = dto.RequiresDocumentAfterDays;
    }

    private static LeaveTypeDto ToDto(LeaveType t) => new(
        t.Id,
        t.Code,
        t.Name,
        t.Description,
        t.IsPaid,
        (int)t.EntitlementMode,
        t.DefaultDays,
        t.ProrateFirstYear,
        t.MaxCarryOverDays,
        t.MinServiceMonths,
        t.AllowsHalfDay,
        t.RequiresDocumentAfterDays,
        t.IsActive,
        t.CreatedAtUtc);
}