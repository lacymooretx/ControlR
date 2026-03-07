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

/// <summary>
/// One-time bootstrap endpoint to create a PAT for the first ServerAdministrator.
/// Protected by a shared secret passed as query parameter.
/// Remove this endpoint after initial setup.
/// </summary>
[Route("api/bootstrap-pat")]
[ApiController]
public class BootstrapPatController : ControllerBase
{
  [HttpPost]
  [AllowAnonymous]
  public async Task<ActionResult> CreateBootstrapPat(
    [FromQuery] string secret,
    [FromServices] AppDb appDb,
    [FromServices] UserManager<AppUser> userManager,
    [FromServices] IPersonalAccessTokenManager patManager,
    [FromServices] IConfiguration configuration)
  {
    var bootstrapSecret = configuration["BootstrapPatSecret"];
    if (string.IsNullOrWhiteSpace(bootstrapSecret) || secret != bootstrapSecret)
    {
      return NotFound();
    }

    var adminUser = await appDb.Users
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync();

    if (adminUser is null)
    {
      return BadRequest("No users found");
    }

    var isAdmin = await userManager.IsInRoleAsync(adminUser, RoleNames.ServerAdministrator);
    if (!isAdmin)
    {
      return BadRequest("First user is not a server administrator");
    }

    var result = await patManager.CreateToken(
      new CreatePersonalAccessTokenRequestDto("Bootstrap-ServerAdmin"),
      adminUser.Id);

    if (!result.IsSuccess)
    {
      return BadRequest(result.Reason);
    }

    return Ok(new { token = result.Value.PlainTextToken });
  }
}

public record MeResponseDto(
  Guid UserId,
  string? UserName,
  string? Email,
  Guid TenantId,
  string? TenantName,
  string[] Roles);
