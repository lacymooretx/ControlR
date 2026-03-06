using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MeController : ControllerBase
{
  [HttpGet]
  public async Task<ActionResult<MeResponseDto>> GetMe(
    [FromServices] UserManager<AppUser> userManager)
  {
    var user = await userManager.GetUserAsync(User);
    if (user is null)
    {
      return BadRequest("User not found");
    }

    var roles = await userManager.GetRolesAsync(user);

    return Ok(new MeResponseDto(
      user.Id,
      user.UserName,
      user.Email,
      user.TenantId,
      roles.ToArray()));
  }
}

public record MeResponseDto(
  Guid UserId,
  string? UserName,
  string? Email,
  Guid TenantId,
  string[] Roles);
