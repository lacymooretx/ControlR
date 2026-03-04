using ControlR.Libraries.Shared.Dtos.ServerApi;
using Microsoft.AspNetCore.Mvc;

namespace ControlR.Web.Server.Api;

[Route("api/alerts")]
[ApiController]
[Authorize]
public class AlertsController : ControllerBase
{
  [HttpPost("{alertId:guid}/acknowledge")]
  [Authorize(Roles = $"{RoleNames.TenantAdministrator}")]
  public async Task<ActionResult<AlertDto>> AcknowledgeAlert(
    [FromServices] AppDb appDb,
    [FromRoute] Guid alertId)
  {
    var alert = await appDb.Alerts
      .Include(x => x.AlertRule)
      .FirstOrDefaultAsync(x => x.Id == alertId);

    if (alert is null)
    {
      return NotFound();
    }

    alert.AcknowledgedAt = DateTimeOffset.UtcNow;
    await appDb.SaveChangesAsync();

    return Ok(alert.ToDto());
  }

  [HttpGet]
  public async Task<ActionResult<AlertDto[]>> GetAlerts(
    [FromServices] AppDb appDb,
    [FromQuery] string? status = null,
    [FromQuery] int count = 50)
  {
    IQueryable<Alert> query = appDb.Alerts
      .AsNoTracking()
      .Include(x => x.AlertRule);

    if (!string.IsNullOrWhiteSpace(status))
    {
      query = query.Where(x => x.Status == status);
    }

    var alerts = await query
      .OrderByDescending(x => x.TriggeredAt)
      .Take(count)
      .ToListAsync();

    var dtos = alerts.Select(x => x.ToDto()).ToArray();
    return Ok(dtos);
  }

  [HttpPost("{alertId:guid}/resolve")]
  [Authorize(Roles = $"{RoleNames.TenantAdministrator}")]
  public async Task<ActionResult<AlertDto>> ResolveAlert(
    [FromServices] AppDb appDb,
    [FromRoute] Guid alertId)
  {
    var alert = await appDb.Alerts
      .Include(x => x.AlertRule)
      .FirstOrDefaultAsync(x => x.Id == alertId);

    if (alert is null)
    {
      return NotFound();
    }

    alert.Status = "Resolved";
    alert.ResolvedAt = DateTimeOffset.UtcNow;
    await appDb.SaveChangesAsync();

    return Ok(alert.ToDto());
  }
}
