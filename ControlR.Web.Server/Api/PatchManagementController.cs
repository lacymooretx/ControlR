using ControlR.Libraries.Shared.Dtos.ServerApi;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/patch-management")]
[ApiController]
[Authorize]
public class PatchManagementController : ControllerBase
{
  [HttpGet("pending")]
  public async Task<ActionResult<PendingPatchDto[]>> GetPendingPatches(
    [FromServices] AppDb appDb,
    [FromQuery] Guid? deviceId = null)
  {
    IQueryable<PendingPatch> query = appDb.PendingPatches
      .AsNoTracking();

    if (deviceId is not null)
    {
      query = query.Where(p => p.DeviceId == deviceId);
    }

    var patches = await query
      .Where(p => p.Status == "Pending")
      .OrderByDescending(p => p.IsCritical)
      .ThenByDescending(p => p.IsImportant)
      .ThenBy(p => p.Title)
      .ToListAsync();

    var dtos = patches.Select(p => p.ToDto()).ToArray();
    return Ok(dtos);
  }

  [HttpGet("pending/{deviceId:guid}")]
  public async Task<ActionResult<PendingPatchDto[]>> GetDevicePendingPatches(
    [FromServices] AppDb appDb,
    [FromRoute] Guid deviceId)
  {
    var patches = await appDb.PendingPatches
      .AsNoTracking()
      .Where(p => p.DeviceId == deviceId && p.Status == "Pending")
      .OrderByDescending(p => p.IsCritical)
      .ThenByDescending(p => p.IsImportant)
      .ThenBy(p => p.Title)
      .ToListAsync();

    var dtos = patches.Select(p => p.ToDto()).ToArray();
    return Ok(dtos);
  }

  [HttpGet("installations")]
  public async Task<ActionResult<PatchInstallationDto[]>> GetInstallations(
    [FromServices] AppDb appDb,
    [FromQuery] Guid? deviceId = null,
    [FromQuery] int count = 50)
  {
    IQueryable<PatchInstallation> query = appDb.PatchInstallations
      .AsNoTracking();

    if (deviceId is not null)
    {
      query = query.Where(i => i.DeviceId == deviceId);
    }

    var installations = await query
      .OrderByDescending(i => i.InitiatedAt)
      .Take(count)
      .ToListAsync();

    var dtos = installations.Select(i => i.ToDto()).ToArray();
    return Ok(dtos);
  }

  [HttpPost("scan")]
  [Authorize(Roles = $"{RoleNames.TenantAdministrator}")]
  public ActionResult TriggerScan([FromBody] PatchScanRequestDto request)
  {
    // Scan is triggered via SignalR hub (ViewerHub.RequestPatchScan).
    // This endpoint exists for REST API completeness / future use.
    return Ok();
  }

  [HttpPost("install")]
  [Authorize(Roles = $"{RoleNames.TenantAdministrator}")]
  public ActionResult TriggerInstall([FromBody] PatchInstallRequestDto request)
  {
    // Install is triggered via SignalR hub (ViewerHub.RequestPatchInstall).
    // This endpoint exists for REST API completeness / future use.
    return Ok();
  }
}
