using ControlR.Libraries.Shared.Dtos.ServerApi;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/scripts")]
[ApiController]
[Authorize]
public class ScriptsController : ControllerBase
{
  [HttpPost]
  [Authorize(Roles = $"{RoleNames.TenantAdministrator}")]
  public async Task<ActionResult<SavedScriptDto>> CreateScript(
    [FromServices] AppDb appDb,
    [FromBody] SavedScriptCreateRequestDto dto)
  {
    if (!User.TryGetTenantId(out var tenantId))
    {
      return NotFound("User tenant not found.");
    }

    if (!User.TryGetUserId(out var userId))
    {
      return Unauthorized();
    }

    var script = new SavedScript
    {
      CreatorUserId = userId,
      Description = dto.Description,
      IsPublishedToClients = dto.IsPublishedToClients,
      Name = dto.Name,
      ScriptContent = dto.ScriptContent,
      ScriptType = dto.ScriptType,
      TenantId = tenantId,
    };

    await appDb.SavedScripts.AddAsync(script);
    await appDb.SaveChangesAsync();

    return Ok(script.ToDto());
  }

  [HttpDelete("{scriptId:guid}")]
  [Authorize(Roles = $"{RoleNames.TenantAdministrator}")]
  public async Task<ActionResult> DeleteScript(
    [FromServices] AppDb appDb,
    [FromRoute] Guid scriptId)
  {
    var script = await appDb.SavedScripts
      .FirstOrDefaultAsync(x => x.Id == scriptId);

    if (script is null)
    {
      return NotFound();
    }

    appDb.SavedScripts.Remove(script);
    await appDb.SaveChangesAsync();

    return NoContent();
  }

  [HttpGet]
  public async Task<ActionResult<SavedScriptDto[]>> GetAllScripts(
    [FromServices] AppDb appDb)
  {
    var isClientUser = User.IsInRole(RoleNames.ClientUser);

    var query = appDb.SavedScripts.AsNoTracking();

    if (isClientUser)
    {
      query = query.Where(x => x.IsPublishedToClients);
    }

    var scripts = await query
      .OrderBy(x => x.Name)
      .ToListAsync();

    var dtos = scripts.Select(x => x.ToDto()).ToArray();
    return Ok(dtos);
  }

  [HttpGet("{scriptId:guid}")]
  public async Task<ActionResult<SavedScriptDto>> GetScript(
    [FromServices] AppDb appDb,
    [FromRoute] Guid scriptId)
  {
    var script = await appDb.SavedScripts
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == scriptId);

    if (script is null)
    {
      return NotFound();
    }

    var isClientUser = User.IsInRole(RoleNames.ClientUser);
    if (isClientUser && !script.IsPublishedToClients)
    {
      return Forbid();
    }

    return Ok(script.ToDto());
  }

  [HttpPut]
  [Authorize(Roles = $"{RoleNames.TenantAdministrator}")]
  public async Task<ActionResult<SavedScriptDto>> UpdateScript(
    [FromServices] AppDb appDb,
    [FromBody] SavedScriptUpdateRequestDto dto)
  {
    var script = await appDb.SavedScripts
      .FirstOrDefaultAsync(x => x.Id == dto.Id);

    if (script is null)
    {
      return NotFound();
    }

    script.Description = dto.Description;
    script.IsPublishedToClients = dto.IsPublishedToClients;
    script.Name = dto.Name;
    script.ScriptContent = dto.ScriptContent;
    script.ScriptType = dto.ScriptType;

    await appDb.SaveChangesAsync();

    return Ok(script.ToDto());
  }
}
