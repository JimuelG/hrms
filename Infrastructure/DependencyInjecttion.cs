using Application.Features.Platform;
using Application.Features.Settings;
using Core.Interfaces;
using Infrastructure.Authorization;
using Infrastructure.Common;
using Infrastructure.Features;
using Infrastructure.Features.Platform;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Storage;
using Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;
public static class DependencyInjecttion
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantContextInitializer>(sp => sp.GetRequiredService<TenantContext>());

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHttpContextAccessor();
        services.AddScoped<SoftDeleteSaveChangesInterceptor>();

        services.AddScoped<ITenantSettingsService, TenantSettingsService>();
        services.AddScoped<ITenantDirectory, TenantDirectory>();
        services.AddScoped<IFeatureGate, FeaturesGate>();
        services.AddScoped<IPlatformTenantService, PlatformTenantService>();
        services.AddScoped<IPlatformSubscriptionService, PlatformSubscriptionService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        
        services.AddOptions<JwtOptions>()
            .Bind(config.GetSection(JwtOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddDataProtection();
        
        services.AddIdentityCore<ApplicationUser>(o =>
        {
            o.Password.RequiredLength = 10;
            o.Password.RequireDigit = true;
            o.Password.RequireLowercase = true;
            o.Password.RequireUppercase = true;
            o.Password.RequireNonAlphanumeric = false;
            o.User.RequireUniqueEmail = true;
            o.Lockout.AllowedForNewUsers = true;
            o.Lockout.MaxFailedAccessAttempts = 5;
            o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

        services.AddSingleton<JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        
        services.AddScoped<TenantSaveChangesInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseSqlServer(config.GetConnectionString("Default")); 
            options.AddInterceptors(
                sp.GetRequiredService<TenantSaveChangesInterceptor>(),
                sp.GetRequiredService<SoftDeleteSaveChangesInterceptor>(),
                sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddMemoryCache();
        services.AddScoped<IPermissionService, PermissionService>();

        return services;
    }
}
