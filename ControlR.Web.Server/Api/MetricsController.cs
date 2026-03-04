using ControlR.Libraries.Shared.Dtos.ServerApi;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/metrics")]
[ApiController]
[Authorize]
public class MetricsController : ControllerBase
{
  [HttpGet("{deviceId:guid}")]
  public async Task<ActionResult<DeviceMetricSnapshotDto[]>> GetDeviceMetrics(
    [FromServices] AppDb appDb,
    [FromRoute] Guid deviceId,
    [FromQuery] int hours = 24)
  {
    var since = DateTimeOffset.UtcNow.AddHours(-hours);

    var snapshots = await appDb.DeviceMetricSnapshots
      .AsNoTracking()
      .Where(x => x.DeviceId == deviceId && x.Timestamp >= since)
      .OrderBy(x => x.Timestamp)
      .ToListAsync();

    var dtos = snapshots.Select(x => new DeviceMetricSnapshotDto(
      x.Timestamp,
      x.CpuPercent,
      x.MemoryPercent,
      x.DiskPercent)).ToArray();

    return Ok(dtos);
  }
}
