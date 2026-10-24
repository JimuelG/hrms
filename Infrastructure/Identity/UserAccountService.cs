using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Core.Common;
using Core.DTOs.Accounts;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client.NativeInterop;

namespace Infrastructure.Identity;

public sealed class UserAccountService(
    AppDbContext db,
    UserManager<ApplicationUser> users,
    ITenantContext tenant,
    ICurrentUserService currentUser,
    TimeProvider clock) : IUserAccountService
{
    private const string EmployeeRoleName = "Employee";
    public async Task<ServiceResult<IssuedLoginDto>> CreateAsync(Guid employeeId, string? email, CancellationToken ct = default)
    {
        var tenantId = tenant.RequiredTenantId;
        var now = clock.GetUtcNow().UtcDateTime;

        var employee = await db.Set<Employee>().FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee is null) return Fail("Employee not found.", ServiceErrorType.NotFound);
        if (employee.UserId is not null) return Fail("This employee already has a login.", ServiceErrorType.Conflict);
        if (AccountRules.ShouldLoseAccess(employee.Status, employee.LastWorkingDay, DateOnly.FromDateTime(now)))
            return Fail("This employee has left, so they can't be given a login.");

        var address = (string.IsNullOrWhiteSpace(email) ? employee.Email : email).Trim();
        if (!new EmailAddressAttribute().IsValid(address)) return Fail("Enter a valid email address.");

        var role = await db.Set<Role>().FirstOrDefaultAsync(r => r.Name == EmployeeRoleName, ct);
        if (role is null) return Fail($"This company has no '{EmployeeRoleName}' role. Create it under roles first.");

        var existing = await users.FindByEmailAsync(address);
        if (existing is not null)
        {
            if (existing.IsPlatformAdmin) return Fail("That email belongs to a platform administrator and can't be linked.");
            if (await db.Set<Employee>().AnyAsync(e => e.UserId == existing.Id, ct))
                return Fail("That login is already linked to another employee in this company.", ServiceErrorType.Conflict);
        }

        string? temporary = null;
        DateTime? expires = null;
        var strategy = db.Database.CreateExecutionStrategy();
        var error = await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var user = existing;
            if (user is null)
            {
                temporary = TemporaryPassword.Generate();
                expires = now + AccountRules.TemporaryPasswordLifetime;
                user = new ApplicationUser
                {
                    UserName = address,
                    Email = address,
                    EmailConfirmed = true,
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    MustChangePassword = true,
                    TemporaryPasswordExpiresAtUtc = expires
                };
                var created = await users.CreateAsync(user, temporary);
                if (!created.Succeeded) return string.Join(" ", created.Errors.Select(e => e.Description));
            }

            var membership = await db.UserTenants.FirstOrDefaultAsync(ut => ut.UserId == user.Id && ut.TenantId == tenantId, ct);
            if (membership is null) db.UserTenants.Add(new UserTenant { UserId = user.Id, TenantId = tenantId });
            else membership.IsActive = true;

            var hasRole = await db.Set<UserTenantRole>().AnyAsync(r => r.UserId == user.Id && r.TenantId == tenantId && r.RoleId == role.Id, ct);
            if (!hasRole) db.Set<UserTenantRole>().Add(new UserTenantRole { UserId = user.Id, TenantId = tenantId, RoleId = role.Id });

            employee.UserId = user.Id;
            Audit(user.Id, existing is null ? "LoginCreated" : "LoginLinked", address);

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return null;
        });

        if (error is not null) return Fail(error);
        return ServiceResult<IssuedLoginDto>.Success(new IssuedLoginDto(address, temporary, expires, existing is not null));
    }

    public async Task<ServiceResult<LoginAccountDto>> GetAsync(Guid employeeId, CancellationToken ct = default)
    {
        var employee = await db.Set<Employee>().FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee is null) return ServiceResult<LoginAccountDto>.Fail("Employee not found.", ServiceErrorType.NotFound);

        var none = new LoginAccountDto(false, null, false, false, false);
        if (employee.UserId is not Guid uid) return ServiceResult<LoginAccountDto>.Success(none);
        var user = await users.FindByIdAsync(uid.ToString());
        if (user is null) return ServiceResult<LoginAccountDto>.Success(none);

        var tenantId = tenant.RequiredTenantId;
        var here = await db.UserTenants.FirstOrDefaultAsync(ut => ut.UserId == uid && ut.TenantId == tenantId, ct);
        var elsewhere = await db.UserTenants.AnyAsync(ut => ut.UserId == uid && ut.TenantId != tenantId && ut.IsActive, ct);

        return ServiceResult<LoginAccountDto>.Success(
                new LoginAccountDto(true, user.Email, here?.IsActive ?? false, user.MustChangePassword, elsewhere));
    }

    public async Task<ServiceResult<IssuedLoginDto>> ResetPasswordAsync(Guid employeeId, CancellationToken ct = default)
    {
        var tenantId = tenant.RequiredTenantId;
        var employee = await db.Set<Employee>().FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee is null) return Fail("Employee not found.", ServiceErrorType.NotFound);
        if (employee.UserId is not Guid uid) return Fail("This employee has no login yet.");

        var user = await users.FindByIdAsync(uid.ToString());
        if (user is null) return Fail("This employee's login no longer exist.");

        var here = await db.UserTenants.AnyAsync(ut => ut.UserId == uid && ut.TenantId == tenantId && ut.IsActive, ct);
        if (!here) return Fail("This login is switched off for this company.");

        var active = await db.UserTenants.CountAsync(ut => ut.UserId == uid && ut.IsActive, ct);
        var refusal = AccountRules.ResetRefusal(user.IsPlatformAdmin, active);
        if (refusal is not null) return Fail(refusal, ServiceErrorType.Conflict);

        var temporary = TemporaryPassword.Generate();
        var expires = clock.GetUtcNow().UtcDateTime + AccountRules.TemporaryPasswordLifetime;

        var strategy = db.Database.CreateExecutionStrategy();
        var error = await strategy.ExecuteAsync(async () =>
        {
           await using var tx = await db.Database.BeginTransactionAsync(ct);

           var removed = await users.RemovePasswordAsync(user);
           if (!removed.Succeeded) return string.Join(" ", removed.Errors.Select(e => e.Description));
           var added = await users.AddPasswordAsync(user, temporary);
           if (!added.Succeeded) return string.Join(" ", added.Errors.Select(e => e.Description));

           user.MustChangePassword = true;
           user.TemporaryPasswordExpiresAtUtc = expires;
           await users.UpdateAsync(user);
           await users.SetLockoutEndDateAsync(user, null);
           await users.ResetAccessFailedCountAsync(user);

           await db.RefreshTokens.Where(t => t.UserId == uid && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, DateTime.UtcNow), ct);

            Audit(uid, "PasswordReset", user.Email ?? "");
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return null; 
        });

        if (error is not null) return Fail(error);
        return ServiceResult<IssuedLoginDto>.Success(new IssuedLoginDto(user.Email ?? "", temporary, expires, false));

    }

    private void Audit(Guid userId, string action, string email) =>
        db.AuditLogs.Add(new AuditLog
        {
           Id = Guid.NewGuid(),
           TenantId = tenant.RequiredTenantId,
           UserId = currentUser.UserId,
           EntityName = "LoginAccount",
           EntityId = userId.ToString(),
           Action = action,
           NewValues = JsonSerializer.Serialize(new { Email = email }),
           IpAddress = currentUser.IpAddress,
           UserAgent = currentUser.UserAgent,
           TimestampUtc = clock.GetUtcNow().UtcDateTime 
        });

    private static ServiceResult<IssuedLoginDto> Fail(string message, ServiceErrorType type = ServiceErrorType.Validation) =>
        ServiceResult<IssuedLoginDto>.Fail(message, type);
}