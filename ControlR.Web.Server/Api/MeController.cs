using ControlR.Web.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlR.Web.Server.Api;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MeController : ControllerBase
{
  [HttpGet]
  public async Task<ActionResult<MeResponseDto>> GetMe(
    [FromServices] UserManager<AppUser> userManager,
    [FromServices] AppDb appDb)
  {
    var user = await userManager.GetUserAsync(User);
    if (user is null)
    {
      return BadRequest("User not found");
    }

    var roles = await userManager.GetRolesAsync(user);

    var tenant = await appDb.Tenants.FirstOrDefaultAsync(t => t.Id == user.TenantId);

    return Ok(new MeResponseDto(
      user.Id,
      user.UserName,
      user.Email,
      user.TenantId,
      tenant?.Name,
      roles.ToArray()));
  }
}

public record MeResponseDto(
  Guid UserId,
  string? UserName,
  string? Email,
  Guid TenantId,
  string? TenantName,
  string[] Roles);
