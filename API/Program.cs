using System.Text.Json.Serialization;
using API.Extensions;
using API.Middleware;
using Application;
using Infrastructure;
using Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
        policy.WithOrigins("http://localhost:4200","https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());    
            
});
builder.Services.AddApplication();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await DevDataSeeder.SeedAsync(app.Services, app.Configuration);
}

app.UseCors("AngularDev");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<FeatureGateExceptionMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program {}