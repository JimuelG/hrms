using Core.Common;
using Microsoft.AspNetCore.Mvc;

namespace API.Extensions;
public static class ServiceResultExtensions
{
    public static IActionResult ToActionResult<T>(this ServiceResult<T> result, ControllerBase controller, bool isCreate = false)
    {
        if (result.Succeeded)
            return isCreate ? controller.StatusCode(201, result.Value) : controller.Ok(result.Value);

        return result.ErrorType switch
        {
            ServiceErrorType.NotFound => controller.NotFound(new { message = result.Error }),
            ServiceErrorType.Conflict => controller.Conflict(new { message = result.Error }),
            _ => controller.BadRequest(new { message = result.Error })
        };
    }
}