using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ControlR.Web.Server.Middleware;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequiresVerificationAttribute : Attribute, IAsyncActionFilter
{
  public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
  {
    var verificationService = context.HttpContext.RequestServices.GetRequiredService<IActionVerificationService>();

    if (!context.HttpContext.User.TryGetUserId(out var userId))
    {
      context.Result = new UnauthorizedResult();
      return;
    }

    if (!verificationService.IsVerified(userId))
    {
      context.Result = new ObjectResult(new { error = "Action verification required", code = "VERIFICATION_REQUIRED" })
      {
        StatusCode = StatusCodes.Status403Forbidden
      };
      return;
    }

    await next();
  }
}
