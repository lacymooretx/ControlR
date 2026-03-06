using ControlR.Libraries.Shared.Dtos.ServerApi;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/jit-admin")]
[ApiController]
[Authorize(Roles = RoleNames.TenantAdministrator)]
public class JitAdminController : ControllerBase
{
  [HttpGet]
  public async Task<ActionResult<JitAdminAccountDto[]>> GetJitAdminAccounts(
    [FromServices] AppDb appDb)
  {
    var accounts = await appDb.JitAdminAccounts
      .AsNoTracking()
      .OrderByDescending(x => x.CreatedAt)
      .ToListAsync();

    return Ok(accounts.Select(a => a.ToDto()).ToArray());
  }

  [HttpGet("{id:guid}")]
  public async Task<ActionResult<JitAdminAccountDto>> GetJitAdminAccount(
    [FromServices] AppDb appDb,
    [FromRoute] Guid id)
  {
    var account = await appDb.JitAdminAccounts
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == id);

    if (account is null)
    {
      return NotFound();
    }

    return Ok(account.ToDto());
  }
}
