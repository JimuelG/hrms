using Core.Constant;
using Core.Entities;
using Core.Enums;
using Infrastructure.Identity;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence;
public static class DevDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var tenantInit = scope.ServiceProvider.GetRequiredService<ITenantContextInitializer>();

        var password = config["Seed:Password"]
            ?? throw new InvalidOperationException("Set Seed:Password in user-secrets.");
        
        async Task<Tenant> EnsureTenant(string slug, string name)
        {
            var t = await db.Tenants.FirstOrDefaultAsync(x => x.Slug == slug);
            if (t is null) { t = new Tenant { Name = name, Slug = slug }; db.Tenants.Add(t); await db.SaveChangesAsync(); }
            return t;
        }

        async Task<ApplicationUser> EnsureUser(string email, bool plaformAdmin, params Tenant[] memberOf)
        {
            var existing = await users.FindByEmailAsync(email);

            if (existing is not null) return existing;

            var u = new ApplicationUser
            {
                UserName = email, 
                Email = email, 
                EmailConfirmed = true,
                FirstName = "Dev", 
                LastName = email.Split('@')[0], 
                IsPlatformAdmin = plaformAdmin 
            };

            var r = await users.CreateAsync(u, password);
            if (!r.Succeeded) throw new InvalidOperationException(string.Join("; ", r.Errors.Select(e => e.Description)));
            foreach (var t in memberOf) db.UserTenants.Add(new UserTenant { UserId = u.Id, TenantId = t.Id });
            await db.SaveChangesAsync();
            return u;
        }

        async Task<SubscriptionPlan> EnsurePlan(string code, string name, int employees, int branches, int departments, int hrUsers, PlanFeature features)
        {
            var p = await db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Code == code);
            if (p is null)
            {
                p = new SubscriptionPlan
                {
                    Code = code,
                    Name = name,
                    MaxEmployees = employees,
                    MaxBranches = branches,
                    MaxDepartments = departments,
                    MaxHrUsers = hrUsers,
                    Features = features
                };

                db.SubscriptionPlans.Add(p);
                await db.SaveChangesAsync();
            }

            return p;
        }

        var acme = await EnsureTenant("acme", "Acme Corp");
        var globex = await EnsureTenant("globex", "Globex Inc");

        var acmeAdmin = await EnsureUser("admin@acme.test", false, acme);
        await EnsureUser("multi@hrms.test", false, acme, globex);
        await EnsureUser("platform@hrms.test", true);
        var acmeHr = await EnsureUser("hr@acme.test", false, acme);

        var existingCodes = await db.Permissions.Select(p => p.Code).ToListAsync();
        var missing = Permissions.All().Where(p => !existingCodes.Contains(p.Code)).ToList();
        if (missing.Count > 0)
        {
            db.Permissions.AddRange(missing.Select(p => new Permission
            {
                Code = p.Code,
                Module = p.Module,
                Description = p.Description
            }));
            await db.SaveChangesAsync();
        }

        var starter = await EnsurePlan("starter", "Starter", employees: 50, branches: 1, departments: 10, hrUsers: 2, PlanFeature.None);
        var professional = await EnsurePlan("professional", "Professional", employees: 250, branches: 5, departments: 50, hrUsers: 10, PlanFeature.Ats | PlanFeature.Payroll | PlanFeature.Benefits);
        await EnsurePlan("business", "Business", employees: 1000, branches: -1, departments: -1, hrUsers: 50, PlanFeature.Ats | PlanFeature.Payroll | PlanFeature.Benefits | PlanFeature.AdvancedAnalytics | PlanFeature.BiometricAttendance);
        await EnsurePlan("enterprise", "Enterprise", employees: -1, branches: -1, departments: -1, hrUsers: -1, (PlanFeature)63);

        async Task EnsureSubscription(Tenant t, SubscriptionPlan plan)
        {
            tenantInit.SetTenant(t.Id);
            var exists = await db.TenantSubscriptions.AnyAsync(s => s.TenantId == t.Id);
            if (!exists)
            {
                db.TenantSubscriptions.Add(new TenantSubscription
                {
                   TenantId = t.Id,
                   SubscriptionPlanId = plan.Id,
                   Status = TenantSubscriptionStatus.Active 
                });

                await db.SaveChangesAsync();
            }
        }

        async Task EnsureOnboardingTemplates(Tenant t)
        {
            tenantInit.SetTenant(t.Id);
            var hasAny = await db.OnboardingTaskTemplates.AnyAsync(x => x.TenantId == t.Id);
            if (hasAny) return;

            db.OnboardingTaskTemplates.AddRange(
                new OnboardingTaskTemplate { Title = "Verify government ID", Description = "Confirm a valid government-issued ID is on file.", IsRequired = true, SortOrder = 1 },
                new OnboardingTaskTemplate { Title = "Collect signed employement contract", Description = "Signed copy uploaded to employee documents.", IsRequired = true, SortOrder = 2 },
                new OnboardingTaskTemplate { Title = "Collect tax/government forms", Description = "Statutory forms required for payroll setup.", IsRequired = true, SortOrder = 3 },
                new OnboardingTaskTemplate { Title = "Set up IT accounts", Description = "Email, system access, and hardware provisioning.", IsRequired = true, SortOrder = 4 },
                new OnboardingTaskTemplate { Title = "Assign workstation / equipment", Description = null, IsRequired = false, SortOrder = 5 },
                new OnboardingTaskTemplate { Title = "Schedule orientation session", Description = "Company policies, culture, and expectations walkthrough", IsRequired = true, SortOrder = 6 },
                new OnboardingTaskTemplate { Title = "Introduce tp team and manager", Description = null, IsRequired = false, SortOrder = 7 },
                new OnboardingTaskTemplate { Title = "Enroll in benefits (if eligible)", Description = "HMO and other benefit enrollment, subject to eligibility rules.", IsRequired = false, SortOrder = 8 }
            );

            await db.SaveChangesAsync();
        }

        async Task EnsureWorkLocations(Tenant t)
        {
            tenantInit.SetTenant(t.Id);
            if (await db.WorkLocations.AnyAsync(x => x.TenantId == t.Id)) return;

            db.WorkLocations.AddRange(
                new WorkLocation { Name = "Head Office", Type = WorkLocationType.Office, Latitude = 120.5963, RadiusMeters = 150 },
                new WorkLocation { Name = "Remote / Work from home", Type = WorkLocationType.Remote });

            await db.SaveChangesAsync();
        }

        async Task EnsureLeaveTypes(Tenant t)
        {
            tenantInit.SetTenant(t.Id);
            if (await db.LeaveTypes.AnyAsync(x => x.TenantId == t.Id)) return;

            db.LeaveTypes.AddRange(
                new LeaveType { Code = "VL", Name = "Vacation Leave", EntitlementMode = LeaveEntitlementMode.Annual,
                    DefaultDays = 15, ProrateFirstYear = true, MaxCarryOverDays = 5 },
                new LeaveType { Code = "SL", Name = "Sick Leave", EntitlementMode = LeaveEntitlementMode.Annual,
                    DefaultDays = 10, ProrateFirstYear = true, RequiresDocumentAfterDays = 2 },
                new LeaveType { Code = "SIL", Name = "Service Incentive Leave", EntitlementMode = LeaveEntitlementMode.Annual,
                    Description = "Statutory minimum after one year of service. Inactive by default: a company that already grants 5 or more vacation days usually satisfies it, so turning both on would double-grant.",
                    DefaultDays = 5, ProrateFirstYear = false, MinServiceMonths = 12, IsActive = false },
                new LeaveType { Code = "ML", Name = "Maternity Leave", EntitlementMode = LeaveEntitlementMode.HrGranted,
                    Description = "Granted by HR to employees who qualify.", DefaultDays = 105, AllowsHalfDay = false },
                new LeaveType { Code = "PL", Name = "Paternity Leave", EntitlementMode = LeaveEntitlementMode.HrGranted,
                    Description = "Granted by HR to employees who qualify.", DefaultDays = 7, AllowsHalfDay = false },
                new LeaveType { Code = "SPL", Name = "Solo Parent Leave", EntitlementMode = LeaveEntitlementMode.HrGranted,
                    Description = "Granted by HR to employees who qualify.", DefaultDays = 7, AllowsHalfDay = false },
                new LeaveType { Code = "UL", Name = "Unpaid Leave", IsPaid = false, EntitlementMode = LeaveEntitlementMode.Unlimited });
            await db.SaveChangesAsync();
        }

        async Task EnsureHolidays(Tenant t)
        {
            tenantInit.SetTenant(t.Id);
            if (await db.Holidays.AnyAsync(x => x.TenantId == t.Id)) return;

            var fixedDays = new (int Month, int Day, string Name)[]
            {
                (1, 1, "New Year's Day"), (5, 1, "Labor Day"), (6, 12, "Independence Day"),
                (11, 30, "Bonifacio Day"), (12, 25, "Christmas Day"), (12, 30, "Rizal Day")
            };
            var year = DateTime.UtcNow.Year;
            foreach (var y in new[] { year, year + 1 })
                foreach (var (month, day, name) in fixedDays)
                    db.Holidays.Add(new Holiday { Date = new DateOnly(y, month, day), Name = name, Type = HolidayType.Public });
            await db.SaveChangesAsync();
        }

        await EnsureLeaveTypes(acme);
        await EnsureLeaveTypes(globex);
        await EnsureHolidays(acme);
        await EnsureHolidays(globex);

        await EnsureWorkLocations(acme);
        await EnsureWorkLocations(globex);

        var acmeEmployeeUser = await EnsureUser("employee@acme.test", false, acme);

        await EnsureTenantRole(db, tenantInit, acme, "Employee", isSystemRole: true,
        [
            Permissions.Attendance.Clock,
            Permissions.Leave.Request
        ], assignTo: acmeEmployeeUser);

        await EnsureOnboardingTemplates(acme);
        await EnsureOnboardingTemplates(globex);

        await EnsureSubscription(acme, professional);
        await EnsureSubscription(globex, starter);

        tenantInit.SetTenant(acme.Id);
        await EnsureTenantRole(db, tenantInit, acme, "HR Admin", isSystemRole: true,
        [
           Permissions.Employees.Read,
           Permissions.Employees.Write,
           Permissions.Employees.Delete,
           Permissions.Employees.LinkUser,
           Permissions.Tenant.ManageRoles,
           Permissions.Tenant.ManageSettings,
           Permissions.Branches.Read,
           Permissions.Branches.Write,
           Permissions.Branches.Delete,
           Permissions.Departments.Read,
           Permissions.Departments.Write,
           Permissions.Departments.Delete,
           Permissions.Positions.Read,
           Permissions.Positions.Write,
           Permissions.Positions.Delete,
           Permissions.JobPosting.Read,
           Permissions.JobPosting.Write,
           Permissions.JobPosting.Delete,
           Permissions.Applicants.Read,
           Permissions.Applicants.Write,
           Permissions.Applicants.Delete,
           Permissions.Applications.Read,
           Permissions.Applications.Write,
           Permissions.Interviews.Read,
           Permissions.Interviews.Write,
           Permissions.JobOffers.Read,
           Permissions.JobOffers.Write,
           Permissions.Onboarding.Read,
           Permissions.Onboarding.Write,
           Permissions.WorkSchedules.Read,
           Permissions.WorkSchedules.Write,
           Permissions.WorkSchedules.Delete,
           Permissions.WorkLocations.Read,
           Permissions.WorkLocations.Write,
           Permissions.WorkLocations.Delete,
           Permissions.Attendance.Clock,
           Permissions.Leave.Request,
           Permissions.Leave.Manage,
           Permissions.Leave.Approve,
           Permissions.Compensation.Read,
           Permissions.Compensation.Write,
           Permissions.Payroll.ReadRules,
           Permissions.Payroll.ManageRules,
           Permissions.Payroll.Read,
           Permissions.Payroll.Run,
           
        ], assignTo:acmeAdmin);

        var multiUser = await users.FindByEmailAsync("multi@hrms.test")
            ?? throw new InvalidOperationException("multi@hrms.test must be seeded before assigning its Globex role.");

        tenantInit.SetTenant(globex.Id);
        await EnsureTenantRole(db, tenantInit, globex, "HR Admin", isSystemRole: true,
        [
            Permissions.Employees.Read,
           Permissions.Employees.Write,
           Permissions.Employees.Delete,
           Permissions.Employees.LinkUser,
           Permissions.Tenant.ManageRoles,
           Permissions.Tenant.ManageSettings,
           Permissions.Branches.Read,
           Permissions.Branches.Write,
           Permissions.Branches.Delete,
           Permissions.Departments.Read,
           Permissions.Departments.Write,
           Permissions.Departments.Delete,
           Permissions.Positions.Read,
           Permissions.Positions.Write,
           Permissions.Positions.Delete,
           Permissions.JobPosting.Read,
           Permissions.JobPosting.Write,
           Permissions.JobPosting.Delete,
           Permissions.Applicants.Read,
           Permissions.Applicants.Write,
           Permissions.Applicants.Delete,
           Permissions.Applications.Read,
           Permissions.Applications.Write,
           Permissions.Interviews.Read,
           Permissions.Interviews.Write,
           Permissions.JobOffers.Read,
           Permissions.JobOffers.Write,
           Permissions.Onboarding.Read,
           Permissions.Onboarding.Write,
           Permissions.WorkSchedules.Read,
           Permissions.WorkSchedules.Write,
           Permissions.WorkSchedules.Delete,
           Permissions.WorkLocations.Read,
           Permissions.WorkLocations.Write,
           Permissions.WorkLocations.Delete,
           Permissions.Attendance.Clock,
           Permissions.Leave.Request,
           Permissions.Leave.Manage,
           Permissions.Leave.Approve,
           Permissions.Compensation.Read,
           Permissions.Compensation.Write,
           Permissions.Payroll.ReadRules,
           Permissions.Payroll.ManageRules,
           Permissions.Payroll.Read,
           Permissions.Payroll.Run,

        ], assignTo: multiUser);

        await EnsureTenantRole(db, tenantInit, acme, "Leave Approver", isSystemRole: true,
        [
            Permissions.Leave.Approve,
            Permissions.Leave.Request,
            Permissions.Attendance.Clock    
        ], assignTo: acmeHr);
    }

    private static async Task EnsureTenantRole(
        AppDbContext db, ITenantContextInitializer tenantInit, Tenant tenant, string roleName, bool isSystemRole,
        string[] permissionCodes, ApplicationUser assignTo)
    {
        tenantInit.SetTenant(tenant.Id);

        var role = await db.Roles.FirstOrDefaultAsync(r => r.TenantId == tenant.Id && r.Name == roleName);
        
        if (role is null)
        {
            role = new Role { TenantId = tenant.Id, Name = roleName, IsSystemRole = isSystemRole };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
        }

        var permissionIds = await db.Permissions
            .Where(p => permissionCodes.Contains(p.Code))
            .Select(p => p.Id)
            .ToListAsync();

        var existingLinks = await db.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        foreach (var permId in permissionIds.Except(existingLinks))
            db.RolePermissions.Add(new RolePermission { TenantId = tenant.Id, RoleId = role.Id, PermissionId = permId});

        var alreadyAssigned = await db.UserTenantRoles.AnyAsync(utr =>
            utr.UserId == assignTo.Id && utr.TenantId == tenant.Id && utr.RoleId == role.Id);
        if (!alreadyAssigned)
            db.UserTenantRoles.Add(new UserTenantRole { UserId = assignTo.Id, TenantId = tenant.Id, RoleId = role.Id });

        await db.SaveChangesAsync();
    }
}