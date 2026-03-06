using ControlR.Libraries.Shared.Constants;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route(HttpConstants.ActionVerificationEndpoint)]
[ApiController]
[Authorize]
public class ActionVerificationController(
  IActionVerificationService verificationService,
  ILogger<ActionVerificationController> logger) : ControllerBase
{
  private static readonly TimeSpan _verificationDuration = TimeSpan.FromMinutes(5);
  private readonly IActionVerificationService _verificationService = verificationService;
  private readonly ILogger<ActionVerificationController> _logger = logger;

  [HttpPost("verify")]
  public async Task<ActionResult<ActionVerificationStatusDto>> Verify(
    [FromBody] ActionVerificationRequestDto request,
    [FromServices] UserManager<AppUser> userManager)
  {
    if (!User.TryGetUserId(out var userId))
    {
      return Unauthorized();
    }

    var user = await userManager.FindByIdAsync(userId.ToString());
    if (user is null)
    {
      return Unauthorized();
    }

    var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
    if (!passwordValid)
    {
      _logger.LogWarning(
        "Action verification failed for user {UserName} ({UserId}): invalid password.",
        user.UserName,
        userId);

      return BadRequest(new ActionVerificationStatusDto(false, null));
    }

    _verificationService.SetVerified(userId, _verificationDuration);
    var expiresAt = _verificationService.GetExpiresAt(userId);

    _logger.LogInformation(
      "Action verification succeeded for user {UserName} ({UserId}). Expires at {ExpiresAt}.",
      user.UserName,
      userId,
      expiresAt);

    return Ok(new ActionVerificationStatusDto(true, expiresAt));
  }

  [HttpGet("status")]
  public ActionResult<ActionVerificationStatusDto> GetStatus()
  {
    if (!User.TryGetUserId(out var userId))
    {
      return Unauthorized();
    }

    var isVerified = _verificationService.IsVerified(userId);
    var expiresAt = _verificationService.GetExpiresAt(userId);

    return Ok(new ActionVerificationStatusDto(isVerified, expiresAt));
  }
}
