using ControlR.Libraries.Shared.Dtos.ServerApi;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/inventory")]
[ApiController]
[Authorize]
public class InventoryController : ControllerBase
{
  [HttpGet("{deviceId:guid}/hardware")]
  public async Task<ActionResult<DeviceHardwareDto>> GetDeviceHardware(
    [FromServices] AppDb appDb,
    [FromRoute] Guid deviceId)
  {
    var device = await appDb.Devices
      .AsNoTracking()
      .FirstOrDefaultAsync(d => d.Id == deviceId);

    if (device is null)
    {
      return NotFound();
    }

    var dto = new DeviceHardwareDto(
      device.SerialNumber,
      device.Manufacturer,
      device.Model,
      device.BiosVersion);

    return Ok(dto);
  }

  [HttpGet("{deviceId:guid}/software")]
  public async Task<ActionResult<SoftwareInventoryItemDto[]>> GetDeviceSoftware(
    [FromServices] AppDb appDb,
    [FromRoute] Guid deviceId)
  {
    var items = await appDb.SoftwareInventoryItems
      .AsNoTracking()
      .Where(x => x.DeviceId == deviceId)
      .OrderBy(x => x.Name)
      .ToListAsync();

    var dtos = items.Select(x => x.ToDto()).ToArray();
    return Ok(dtos);
  }

  [HttpGet("{deviceId:guid}/updates")]
  public async Task<ActionResult<InstalledUpdateDto[]>> GetDeviceUpdates(
    [FromServices] AppDb appDb,
    [FromRoute] Guid deviceId)
  {
    var items = await appDb.InstalledUpdates
      .AsNoTracking()
      .Where(x => x.DeviceId == deviceId)
      .OrderByDescending(x => x.InstalledOn)
      .ToListAsync();

    var dtos = items.Select(x => x.ToDto()).ToArray();
    return Ok(dtos);
  }

  [HttpPost("search")]
  public async Task<ActionResult<InventorySearchResultDto[]>> SearchInventory(
    [FromServices] AppDb appDb,
    [FromBody] InventorySearchRequestDto request)
  {
    if (!string.IsNullOrWhiteSpace(request.SoftwareName))
    {
      var query = appDb.SoftwareInventoryItems
        .AsNoTracking()
        .Join(
          appDb.Devices.AsNoTracking(),
          s => s.DeviceId,
          d => d.Id,
          (s, d) => new { Software = s, Device = d })
        .Where(x => EF.Functions.ILike(x.Software.Name, $"%{request.SoftwareName}%"));

      if (!string.IsNullOrWhiteSpace(request.SoftwareVersion))
      {
        query = query.Where(x => EF.Functions.ILike(x.Software.Version, $"%{request.SoftwareVersion}%"));
      }

      var results = await query
        .Select(x => new InventorySearchResultDto(
          x.Device.Id,
          x.Device.Name,
          x.Software.Name,
          x.Software.Version,
          x.Software.Publisher))
        .Take(100)
        .ToArrayAsync();

      return Ok(results);
    }

    if (!string.IsNullOrWhiteSpace(request.UpdateId))
    {
      var results = await appDb.InstalledUpdates
        .AsNoTracking()
        .Join(
          appDb.Devices.AsNoTracking(),
          u => u.DeviceId,
          d => d.Id,
          (u, d) => new { Update = u, Device = d })
        .Where(x => EF.Functions.ILike(x.Update.UpdateId, $"%{request.UpdateId}%") ||
                     EF.Functions.ILike(x.Update.Title, $"%{request.UpdateId}%"))
        .Select(x => new InventorySearchResultDto(
          x.Device.Id,
          x.Device.Name,
          x.Update.Title,
          x.Update.UpdateId,
          string.Empty))
        .Take(100)
        .ToArrayAsync();

      return Ok(results);
    }

    return Ok(Array.Empty<InventorySearchResultDto>());
  }
}
