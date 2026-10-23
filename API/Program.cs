using System.Text.Json.Serialization;
using API.Extensions;
using API.Middleware;
using Application;
using Hangfire;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication();
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddPermissionAuthorization();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? ["http://localhost:4200l", "https://localhost:4200"];
if (allowedOrigins.Any(o => o.Trim() == "*"))
    throw new InvalidOperationException("A wildcard CORS origin can't be combined with credentials. List the exact origins.");
builder.Services.AddCors(options =>
{
    options.AddPolicy("App", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());    
            
});
builder.Services.AddApplication();
builder.Services.AddProblemDetails();

var keysPath = builder.Configuration["DataProtection:KeysPath"]
                ?? Path.Combine(builder.Environment.ContentRootPath, "keys");
builder.Services.AddDataProtection()
    .SetApplicationName("hrms")
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

var jobsConnection = builder.Configuration.GetConnectionString("Default");
var jobsEnabled = builder.Configuration.GetValue("Jobs:Enabled", true)
                  && !string.IsNullOrWhiteSpace(jobsConnection);
if (jobsEnabled)
{
    builder.Services.AddScoped<API.Jobs.MaintenanceJobs>();
    builder.Services.AddHangfire(c => c
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("Default"),
            new Hangfire.SqlServer.SqlServerStorageOptions { SchemaName = "hangfire", PrepareSchemaIfNecessary = true }));
    builder.Services.AddHangfireServer(o => o.WorkerCount = 2);
}

var authPerMinute = builder.Configuration.GetValue("RateLimits:AuthPerMinute", 10);
var sensitivePerMinute = builder.Configuration.GetValue("RateLimits:SensitivePerMinute", 30);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (ctx, _) =>
    {
        if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
            ctx.HttpContext.Response.Headers.RetryAfter = ((int)retry.TotalSeconds).ToString();
        return ValueTask.CompletedTask;
    };

    options.AddPolicy("auth", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = authPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0}));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h.XContentTypeOptions = "nosniff";
    h.XFrameOptions = "DENY";
    h["Referrer-Policy"] = "no-referrer";
    h["Cross-Origin-Opener-Policy"] = "same-origin";

    if (ctx.Request.Path.StartsWithSegments("/api"))
    {
        h.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
        h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    }
    await next();
});

if (jobsEnabled)
{
    var recurring = app.Services.GetRequiredService<IRecurringJobManager>();
    recurring.AddOrUpdate<API.Jobs.MaintenanceJobs>("alert-scan", j => j.RunAlertScan(CancellationToken.None), Cron.Hourly());
    recurring.AddOrUpdate<API.Jobs.MaintenanceJobs>("leave-grants", j => j.EnsureLeaveGrants(CancellationToken.None), Cron.Daily(1));

    if (app.Environment.IsDevelopment())
        app.MapHangfireDashboard("/hangfire", new DashboardOptions { Authorization = [new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter()] });
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await DevDataSeeder.SeedAsync(app.Services, app.Configuration);
}

app.UseCors("App");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseRateLimiter();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<FeatureGateExceptionMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program {}