using Infrastructure.Tenancy;

namespace API.Middleware;
public class TenantResolutionMiddleware
{
    public const string TenantClaimType = "tenant_id";
    private readonly RequestDelegate _next;
    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;
    public async Task InvokeAsync(HttpContext context, ITenantContextInitializer tenantContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var claim = context.User.FindFirst(TenantClaimType)?.Value;
            if (Guid.TryParse(claim, out var tenantId))
                tenantContext.SetTenant(tenantId);
        }

        await _next(context);
    }
}