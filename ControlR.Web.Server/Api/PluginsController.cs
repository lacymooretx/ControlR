using System.Security.Claims;
using ControlR.Libraries.Shared.Dtos.ServerApi;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/plugins")]
[ApiController]
[Authorize(Roles = RoleNames.TenantAdministrator)]
public class PluginsController : ControllerBase
{
  [HttpPost]
  public async Task<ActionResult<PluginRegistrationDto>> CreatePlugin(
    [FromServices] AppDb appDb,
    [FromServices] IPluginLoaderService pluginLoader,
    [FromBody] CreatePluginRequestDto request)
  {
    var tenantId = User.FindFirstValue("TenantId");
    if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var tenantGuid))
    {
      return BadRequest("Invalid tenant.");
    }

    var registration = new PluginRegistration
    {
      Name = request.Name,
      AssemblyPath = request.AssemblyPath,
      PluginTypeName = request.PluginTypeName,
      IsEnabled = request.IsEnabled,
      ConfigurationJson = request.ConfigurationJson,
      TenantId = tenantGuid,
    };

    await appDb.PluginRegistrations.AddAsync(registration);
    await appDb.SaveChangesAsync();

    var loadedPlugins = pluginLoader.GetLoadedPlugins();
    return Ok(registration.ToDto(loadedPlugins));
  }

  [HttpDelete("{pluginId:guid}")]
  public async Task<ActionResult> DeletePlugin(
    [FromServices] AppDb appDb,
    [FromRoute] Guid pluginId)
  {
    var registration = await appDb.PluginRegistrations.FindAsync(pluginId);
    if (registration is null)
    {
      return NotFound();
    }

    appDb.PluginRegistrations.Remove(registration);
    await appDb.SaveChangesAsync();

    return NoContent();
  }

  [HttpGet]
  public async Task<ActionResult<PluginRegistrationDto[]>> GetAllPlugins(
    [FromServices] AppDb appDb,
    [FromServices] IPluginLoaderService pluginLoader)
  {
    var registrations = await appDb.PluginRegistrations
      .AsNoTracking()
      .OrderBy(p => p.Name)
      .ToListAsync();

    var loadedPlugins = pluginLoader.GetLoadedPlugins();
    var dtos = registrations.Select(r => r.ToDto(loadedPlugins)).ToArray();
    return Ok(dtos);
  }

  [HttpGet("{pluginId:guid}")]
  public async Task<ActionResult<PluginRegistrationDto>> GetPlugin(
    [FromServices] AppDb appDb,
    [FromServices] IPluginLoaderService pluginLoader,
    [FromRoute] Guid pluginId)
  {
    var registration = await appDb.PluginRegistrations
      .AsNoTracking()
      .FirstOrDefaultAsync(p => p.Id == pluginId);

    if (registration is null)
    {
      return NotFound();
    }

    var loadedPlugins = pluginLoader.GetLoadedPlugins();
    return Ok(registration.ToDto(loadedPlugins));
  }

  [HttpPost("reload")]
  public async Task<ActionResult> ReloadPlugins(
    [FromServices] IPluginLoaderService pluginLoader)
  {
    await pluginLoader.LoadPlugins();
    return Ok();
  }

  [HttpPut]
  public async Task<ActionResult<PluginRegistrationDto>> UpdatePlugin(
    [FromServices] AppDb appDb,
    [FromServices] IPluginLoaderService pluginLoader,
    [FromBody] UpdatePluginRequestDto request)
  {
    var registration = await appDb.PluginRegistrations.FindAsync(request.Id);
    if (registration is null)
    {
      return NotFound();
    }

    registration.Name = request.Name;
    registration.AssemblyPath = request.AssemblyPath;
    registration.PluginTypeName = request.PluginTypeName;
    registration.IsEnabled = request.IsEnabled;
    registration.ConfigurationJson = request.ConfigurationJson;

    await appDb.SaveChangesAsync();

    var loadedPlugins = pluginLoader.GetLoadedPlugins();
    return Ok(registration.ToDto(loadedPlugins));
  }
}
