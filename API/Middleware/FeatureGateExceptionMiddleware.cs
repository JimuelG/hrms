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
        catch (DuplicateKeyException)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new { message = "That change was already made by another request. Refresh and try again."});
        }
        catch (ConcurrencyConflictException)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new { message = "This was changed by somehow else a moment ago. Refresh and try again."});
        }
    }
}