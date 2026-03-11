using ControlR.Web.Server.Authn;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ControlR.Web.Server.Middleware;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequiresVerificationAttribute : Attribute, IAsyncActionFilter
{
  public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
  {
    // PAT-authenticated requests bypass action verification since the
    // token itself is a pre-authorized credential.
    var authMethod = context.HttpContext.User.FindFirst(UserClaimTypes.AuthenticationMethod)?.Value;
    if (authMethod == PersonalAccessTokenAuthenticationSchemeOptions.DefaultScheme)
    {
      await next();
      return;
    }

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
