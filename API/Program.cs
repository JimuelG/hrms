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
using API.Health;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using API.Jobs;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole(o =>
    {
        o.IncludeScopes = true;
        o.UseUtcTimestamp = true;
        o.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    });
}

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
                    ?? ["http://localhost:4200", "https://localhost:4200"];
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
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
   o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
   o.ForwardLimit = 1;

   foreach (var ip in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? []) 
    if (IPAddress.TryParse(ip, out var address)) o.KnownProxies.Add(address);
});

var keysPath = builder.Configuration["DataProtection:KeysPath"]
                ?? Path.Combine(builder.Environment.ContentRootPath, "keys");
builder.Services.AddDataProtection()
    .SetApplicationName("hrms")
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
    .AddCheck("keys", new KeysHealthCheck(keysPath), tags: ["ready"]);

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

    options.AddPolicy("sensitive", http => RateLimitPartition.GetFixedWindowLimiter(
        http.User.FindFirst("sub")?.Value ?? http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = sensitivePerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0}));
});

var app = builder.Build();

app.UseForwardedHeaders();

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
    recurring.AddOrUpdate<MaintenanceJobs>("alert-scan", j => j.RunAlertScan(CancellationToken.None), Cron.Hourly());
    recurring.AddOrUpdate<MaintenanceJobs>("leave-grants", j => j.EnsureLeaveGrants(CancellationToken.None), Cron.Daily(1));
    recurring.AddOrUpdate<MaintenanceJobs>("retention", j => j.RunRetention(CancellationToken.None), Cron.Daily(2));
    recurring.AddOrUpdate<MaintenanceJobs>("leaver-access", j => j.RemoveLeaverAccess(CancellationToken.None), Cron.Daily(0, 30));


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

app.Use(async (ctx, next) =>
{
    var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Request");
    using (logger.BeginScope(new Dictionary<string, object?>
    {
        ["TenantId"] = ctx.User.FindFirst("tenant_id")?.Value,
        ["UserId"] = ctx.User.FindFirst("sub")?.Value,
        ["RequestId"] = ctx.TraceIdentifier
    }))
    {
        await next();
    }
});

app.UseRateLimiter();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<FeatureGateExceptionMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });


app.Run();

public partial class Program {}