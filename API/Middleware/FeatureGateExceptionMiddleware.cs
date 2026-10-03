using Core.Common;

namespace API.Middleware;
public class FeatureGateExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (FeatureGateException ex)
        {
            context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
    }
}